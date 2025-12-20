using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Networking.API.UserWebsocket;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using ABI_RC.Systems.UI.UILib;
using cohtml;
using MelonLoader;
using MessagePack;
using TotallyWholesome.Managers;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.TWUI;
using TotallyWholesome.Managers.TWUI.Pages;
using TotallyWholesome.Notification;
using TotallyWholesome.Utils;
using TWNetCommon;
using TWNetCommon.Auth;
using TWNetCommon.BasicMessages;
using TWNetCommon.Data;
using UnityEngine;
using WholesomeLoader;
using Yggdrasil.Network.Framing;
using Yggdrasil.Network.TCP;

namespace TotallyWholesome.Network
{
    public class TWNetClient : TcpClient
    {
        public static TWNetClient Instance;
        public static Action OnTWNetConnected;
        public static Action OnTWNetDisconnected;
        public static Action OnTWNetAuthenticated;
        
        public TWNetListener Listener;
        public bool ExpectedDisconnect;
        public bool Reconnecting;
        public bool HasSentAuth;
        public int ReconnectCount = 0;
        public InstanceInfo LastWorldJoinMessage;
        public MessageResponse DisconnectMessage;
        public string WholesomeLoaderLocation;
        public string TargetInstanceID;
        public bool HasBeenRatelimited;
        public TagData CurrentTagData;
        public bool CanUseTag;

        private Task _clientPollTask;
        private string _targetWorld;
        private object _worldChangeToken;
        private string _inviteID;
        private bool _connectedToGS;
        private DateTime _lastPetFollowFail = DateTime.Now;
        private List<Action> _waitingForGSConnection = new();
        
        private string _host = "potato.moe";
        private int _port = 14007;
        private Auth _authPacket;
        private LengthPrefixFramer _framer = new(16000);

        public TWNetClient()
        {
            Instance = this;

            Listener = new TWNetListener();
            _framer.MessageReceived += FramerReceivedData;

            Patches.EarlyWorldJoin += OnEarlyWorldJoin;
            Patches.OnGameNetworkConnected += OnEarlyWorldJoin;
            OnTWNetAuthenticated += OnAuthWorldCheck;
            Patches.OnWorldLeave += AbortInstanceChange;
            Patches.OnWorldLeave += OnGameNetworkDisconnected;
            Patches.UserJoin += OnUserJoinPhoton;
            Patches.OnChangingInstance += AbortInstanceChange;
            Patches.OnWorldJoin += MoveWaitingForGSToQueue;
            Disconnected += OnDisconnected;
        }

        public void ConnectClient()
        {
            ExpectedDisconnect = false;
            
            var wholesomeLoader = MelonMod.RegisteredMelons.FirstOrDefault(m => m.Info.Name == "WholesomeLoader");

            if (wholesomeLoader == null)
                throw new Exception("You've started TotallyWholesome without WholesomeLoader! TotallyWholesome will not function without WholesomeLoader!");
            
            

            WholesomeLoaderLocation = wholesomeLoader.MelonAssembly.Location;
            
            _authPacket = new Auth()
            {
                DisplayName = TWUtils.GetSelfUsername(),
                UserID = MetaPort.Instance.ownerId,
                TWVersion = BuildInfo.TWVersion + "+" + ThisAssembly.Git.Commit,
                Key = Configuration.JSONConfig.LoginKey,
                WLVersion = wholesomeLoader.Info.Version
            };

            if (_clientPollTask != null) return;

            _clientPollTask = TwTask.Run(ClientLogic);
        }

        public async Task SendAsync<T>(T obj, TWNetMessageType packetId)
        {
            Con.Debug($"[SENDASYNC] {obj}");
            try
            {
                await using var memoryStream = new MemoryStream();
                await using (var writer = new BinaryWriter(memoryStream, Encoding.Default, true))
                {
                    writer.Write(0); // Size to be filled by the framer
                    writer.Write(BitConverter.GetBytes((int)packetId));
                    writer.Write(MessagePackSerializer.Serialize(obj));    
                }
                await SendAsync(_framer.FrameNoAlloc(memoryStream));
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }

        public async Task SendPingAsync()
        {
            await using var memoryStream = new MemoryStream();
            await using (var writer = new BinaryWriter(memoryStream, Encoding.Default, true))
            {
                writer.Write(0); // Size to be filled by the framer
                writer.Write(BitConverter.GetBytes((int)TWNetMessageType.PingPong));
            }
            await SendAsync(_framer.FrameNoAlloc(memoryStream));
        }
        
#region Threading

        private async Task ClientLogic()
        {
            while (!Main.Instance.Quitting)
            {
                try
                {
                    if (!Instance.IsTWNetConnected() && !Instance.ExpectedDisconnect && !Instance.Reconnecting)
                    {
                        Instance.Reconnecting = true;
                        Instance.HasSentAuth = false;

                        Instance.ReconnectCount++;
                        try
                        {
                            Instance.Connect(Instance._host, Instance._port);
                        }
                        catch (Exception)
                        {
                            Instance.ExpectedDisconnect = false;
                            Instance.Reconnecting = false;
                            
                            var reconnectTimeAdjustment = 5000 * (Instance.ReconnectCount / 5) + 1000;

                            Con.Error($"Unable to connect to TWNet! Retrying in {reconnectTimeAdjustment / 1000} seconds... (Attempt {Instance.ReconnectCount})");

                            await Task.Delay(reconnectTimeAdjustment);
                        }
                    }

                    if (Instance.IsTWNetConnected() && !Instance.HasSentAuth)
                    {
                        Instance.HasSentAuth = true;

                        if (Instance.Reconnecting)
                        {
                            Instance.Reconnecting = false;
                            Con.Msg(Instance.ReconnectCount==1?"Connected to TWNet successfully!":$"Reconnected to TWNet after {Instance.ReconnectCount} attempts");
                            Instance.ReconnectCount = 0;
                        }

                        await Instance.SendAsync(Instance._authPacket, TWNetMessageType.Auth);
                    }
                }
                catch (Exception e)
                {
                    Con.Error("An exception has occured within the TWNetClient ClientPollThread!");
                    Con.Error(e);
                }

                await Task.Delay(15);
            }
        }

#endregion

        public void AuthResponse(bool success, string authResp)
        {
            if (!success)
            {
                Con.Error($"Unable to authenticate with TWNet! Reason: {authResp}");
                DisconnectClient();
                return;
            }

            Con.Msg("Successfully connected to TWNet!");
            OnTWNetAuthenticated?.Invoke();
        }

        public void DisconnectClient()
        {
            ExpectedDisconnect = true;

            Disconnect();
        }
        
        protected override void ReceiveData(byte[] buffer, int length)
        {
            _framer.ReceiveData(buffer, length);            
        }

        private void FramerReceivedData(byte[] bytes)
        {
            //Extract packet id
            if (bytes.Length < sizeof(int))
            {
                Con.Error("Connection has sent an invalid packet! Size less then packet id bytes");
                return;
            }

            int packetID = BitConverter.ToInt32(bytes, 0);
            
            byte[] finalBytes = new byte[bytes.Length - sizeof(int)];
            
            Array.Copy(bytes, sizeof(int), finalBytes, 0, finalBytes.Length);
            
            Listener.HandlePacket(this, finalBytes, packetID);
        }

        public bool IsTWNetConnected()
        {
            return Status == ClientStatus.Connected;
        }

        public async Task FollowMaster(string worldID)
        {
            if (!ConfigManager.Instance.IsActive(AccessType.FollowMasterWorldChange, LeadManager.Instance.MasterId)) return;
            
            TargetInstanceID = worldID;
            
            var result = await ApiConnection.MakeRequest<InstanceDetailsResponse>(ApiConnection.ApiOperation.InstanceDetail, new
            {
                instanceID = worldID
            });
            
            if(result.Data == null)
            {
                if (LeadManager.Instance.LastMasterPairKey == null) return;
                
                var response = new InstanceFollowResponse()
                {
                    Key = LeadManager.Instance.LastMasterPairKey,
                    TargetInstanceHash = TWUtils.CreateMD5(worldID)
                };
                    
                await SendAsync(response, TWNetMessageType.InstanceFollowResponse);

                return;
            }
            
            Con.Debug($"Master instance follow triggered, instance was found! {result.Data.Name} - {result.Data.InstanceSettingPrivacy}");
            
            GoToTargetWorld(worldID, result.Data.World.Id);
        }

        public void GoToTargetWorld(string instanceID, string worldID)
        {
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                _targetWorld = worldID;
                TargetInstanceID = instanceID;
                NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has changed instance, you will be following in 10 seconds!", 5f, TWAssets.Handcuffs);

                _worldChangeToken = MelonCoroutines.Start(WaitBeforeSetTargetWorld());
            });
        }

        public void AutoAcceptTargetInvite(string inviteID)
        {
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                _inviteID = inviteID;
                NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has changed instance, you will be following in 10 seconds!", 5f, TWAssets.Handcuffs);

                _worldChangeToken = MelonCoroutines.Start(WaitBeforeSetTargetWorld());
            });
        }

        public void OnInstanceFollowResponse(InstanceFollowResponse packet)
        {
            if (!ConfigManager.Instance.IsActive(AccessType.AllowPetWorldChangeFollow, packet.UserID)) return;

            _waitingForGSConnection.Add(() =>
            {
                if (LeadManager.Instance.LastFollowerPairKeys.Contains(packet.Key) && Friends.FriendsWith(packet.UserID) &&
                    Enum.TryParse<Instances.InstancePrivacyType>(TWUtils.GetCurrentInstancePrivacy(), out var privacy))
                {
                    if (privacy == Instances.InstancePrivacyType.EveryoneCanInvite)
                    {
                        ApiConnection.SendWebSocketRequest(RequestType.InviteSend, new
                        {
                            id = packet.UserID
                        });
                        return;
                    }
                }

                if (DateTime.Now.Subtract(_lastPetFollowFail).TotalSeconds <= 30) return;
            
                _lastPetFollowFail = DateTime.Now;

                NotificationSystem.EnqueueNotification("Totally Wholesome", "One or more of your pets was unable to follow you!", 5f, TWAssets.Alert);
            });

            if (_connectedToGS)
                MoveWaitingForGSToQueue();
        }
        
        private void MoveWaitingForGSToQueue()
        {
            MelonCoroutines.Start(WaitForSecondBeforeFiringInvites());
        }

        private IEnumerator WaitForSecondBeforeFiringInvites()
        {
            yield return new WaitForSeconds(2);
            
            foreach(var action in _waitingForGSConnection)
                Main.Instance.MainThreadQueue.Enqueue(action);
            
            _waitingForGSConnection.Clear();
        }

        public void AbortInstanceChange()
        {
            //Cancel world change if in progress
            if(_worldChangeToken != null)
                MelonCoroutines.Stop(_worldChangeToken);

            _worldChangeToken = null;
        }
        
        private void OnGameNetworkDisconnected()
        {
            _connectedToGS = false;
            _waitingForGSConnection.Clear();
            TargetInstanceID = null;
            _inviteID = null;
        }
        
        private void OnDisconnected(TcpClient arg1, ConnectionCloseType arg2)
        {
            if (DisconnectMessage != null)
            {
                Con.Error($"Connection to TWNet lost! You were disconnected by server for reason: {DisconnectMessage.Message}");
                ExpectedDisconnect = true;
                DisconnectMessage = null;
                OnTWNetDisconnected?.Invoke();
                return;
            }
            
            Con.Warn($"Connection to TWNet lost! Disconnect reason: {arg2}");
            
            TWMenu.Instance.OnlineUsers = -1;
            OnTWNetDisconnected?.Invoke();
        }

        public IEnumerator WaitBeforeSetTargetWorld()
        {
            yield return new WaitForSeconds(10f);

            if (Instances.CurrentInstanceId.Equals(TargetInstanceID)) yield break;
            
            Con.Msg("Set new target world, changing instance!");

            if (_inviteID != null)
            {
                UIMessageManager.Instance.ClearMessageByReferenceID(UIMessageCategory.Invite, _inviteID);
                _inviteID = null;
            }
            
            Instances.TryJoinInstance(TargetInstanceID, Instances.JoinInstanceSource.Mod);
        }

        private void OnAuthWorldCheck()
        {
            if (LastWorldJoinMessage == null) return;

            if (!IsTWNetConnected()) return;

            TwTask.Run(SendAsync(LastWorldJoinMessage, TWNetMessageType.InstanceInfo));
        }
        
        /// <summary>
        /// Send a hash of the user joining the instance
        /// </summary>
        /// <param name="userID"></param>
        private void OnUserJoinPhoton(CVRPlayerEntity player)
        {
            var userJoin = new UserInstanceChange()
            {
                UserIDHash = TWUtils.CreateMD5(player.Uuid)
            };

            if (!IsTWNetConnected()) return;
            
            TwTask.Run(SendAsync(userJoin, TWNetMessageType.UserJoin));
        }

        private void OnEarlyWorldJoin()
        {
            if (string.IsNullOrWhiteSpace(Instances.RequestedInstance))
                return;

            if (_connectedToGS)
                return;
            
            if (Instances.IsHomeInstance)
                return;

            _connectedToGS = true;

            LastWorldJoinMessage = new InstanceInfo
            {
                InstanceHash = TWUtils.CreateMD5(Instances.RequestedInstance)
            };

            if (ConfigManager.Instance.IsActive(AccessType.AllowPetWorldChangeFollow, LeadManager.Instance.MasterId))
                LastWorldJoinMessage.FullInstanceID = Instances.RequestedInstance;

            if (!IsTWNetConnected()) return;

            TwTask.Run(SendAsync(LastWorldJoinMessage, TWNetMessageType.InstanceInfo));
        }

        protected override void OnReceiveException(Exception ex)
        {
            Con.Error(ex);
        }
    }
}