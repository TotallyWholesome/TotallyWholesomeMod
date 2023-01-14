using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Networking.API.UserWebsocket;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using cohtml;
using MelonLoader;
using MessagePack;
using TotallyWholesome.Managers;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Notification;
using TWNetCommon;
using TWNetCommon.Auth;
using TWNetCommon.BasicMessages;
using TWNetCommon.Data;
using UnityEngine;
using WholesomeLoader;
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
        public int OnlineUsers = 0;
        public bool ExpectedDisconnect;
        public bool Reconnecting;
        public bool HasSentAuth;
        public int ReconnectCount = 0;
        public InstanceInfo LastWorldJoinMessage;
        public MessageResponse DisconnectMessage;
        public string WholesomeLoaderLocation;
        public string TargetInstanceID;

        private Thread _clientPollThread;
        private string _targetWorld;
        private object _worldChangeToken;
        private string _inviteID;
        private bool _connectedToGS;
        private DateTime _lastPetFollowFail = DateTime.Now;
        private List<Action> _waitingForGSConnection = new();

        private string _host = "potato.moe";
        private int _port = 14004;
        private Auth _authPacket;

        public TWNetClient()
        {
            Instance = this;

            Listener = new TWNetListener();

            Patches.Patches.EarlyWorldJoin += OnEarlyWorldJoin;
            Patches.Patches.OnGameNetworkConnected += OnEarlyWorldJoin;
            OnTWNetAuthenticated += OnAuthWorldCheck;
            Patches.Patches.OnWorldLeave += AbortInstanceChange;
            Patches.Patches.OnWorldLeave += OnGameNetworkDisconnected;
            Patches.Patches.UserJoin += OnUserJoinPhoton;
            Patches.Patches.OnChangingInstance += OnChangingInstance;
            Patches.Patches.OnWorldJoin += MoveWaitingForGSToQueue;
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
                DisplayName = MetaPort.Instance.username,
                UserID = MetaPort.Instance.ownerId,
                TWVersion = BuildInfo.TWVersion,
                Key = Configuration.JSONConfig.LoginKey,
                WLVersion = wholesomeLoader.Info.Version
            };

            if (_clientPollThread != null) return;
            
            _clientPollThread = new Thread(ClientLogic);
            _clientPollThread.Start();
        }
        
        public void Send(object obj, int packetID)
        {
            try
            {
                using var writer = new BinaryWriter(new MemoryStream());
                writer.Write(BitConverter.GetBytes(packetID));
                
                switch (packetID)
                {
                    case TWNetMessageTypes.Auth:
                        writer.Write(MessagePackSerializer.Serialize((Auth)obj));
                        break;
                    case TWNetMessageTypes.AuthResp:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((AuthResp)obj));
                        break;
                    case TWNetMessageTypes.Disconnection:
                    case TWNetMessageTypes.UserCountUpdated:
                    case TWNetMessageTypes.LeadRequestResp:
                    case TWNetMessageTypes.SystemNotice:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((MessageResponse)obj));
                        break;
                    case TWNetMessageTypes.PairJoinNotification:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((PairJoinNotification)obj));
                        break;
                    case TWNetMessageTypes.LeadRequest:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((LeadRequest)obj));
                        break;
                    case TWNetMessageTypes.LeadAccept:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((LeadAccept)obj));
                        break;
                    case TWNetMessageTypes.InstanceInfo:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((InstanceInfo)obj));
                        break;
                    case TWNetMessageTypes.MasterRemoteControl:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((MasterRemoteControl)obj));
                        break;
                    case TWNetMessageTypes.MasterSettings:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((MasterSettings)obj));
                        break;
                    case TWNetMessageTypes.StatusUpdate:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((StatusUpdate)obj));
                        break;
                    case TWNetMessageTypes.StatusUpdateConfirmation:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((StatusUpdateConfirmation)obj));
                        break;
                    case TWNetMessageTypes.UserJoin:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((UserInstanceChange)obj));
                        break;

                    case TWNetMessageTypes.InstanceFollowResponse:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((InstanceFollowResponse)obj));
                        break;
                    case TWNetMessageTypes.LeashConfigUpdate:
                        Con.Debug($"[SEND] - {obj}");
                        writer.Write(MessagePackSerializer.Serialize((LeashConfigUpdate)obj));
                        break;
                }
                
                writer.Flush();
                
                Send(((MemoryStream)writer.BaseStream).ToArray());
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }

#region Threading

        private static void ClientLogic()
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

                            Thread.Sleep(reconnectTimeAdjustment);
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

                        Instance.Send(Instance._authPacket, TWNetMessageTypes.Auth);
                    }
                }
                catch (Exception e)
                {
                    Con.Error("An exception has occured within the TWNetClient ClientPollThread!");
                    Con.Error(e);
                }

                Thread.Sleep(15);
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
            //Extract packet id
            if (length < sizeof(int))
            {
                Con.Error("Connection has sent an invalid packet! Size less then packet id bytes");
                return;
            }

            byte[] data = new byte[length];
        
            Array.Copy(buffer, data, length);

            int packetID = BitConverter.ToInt32(data, 0);
            
            byte[] finalBytes = new byte[data.Length - sizeof(int)];
            
            Array.Copy(data, sizeof(int), finalBytes, 0, finalBytes.Length);
            
            Listener.HandlePacket(this, finalBytes, packetID, ReceiveData);
        }

        public bool IsTWNetConnected()
        {
            return Status == ClientStatus.Connected;
        }
        
        private void OnChangingInstance()
        {
            if (_worldChangeToken == null)
                return;
            
            MelonCoroutines.Stop(_worldChangeToken);
        }

        public async void FollowMaster(string worldID)
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
                    
                Send(response, TWNetMessageTypes.InstanceFollowResponse);

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
                    Enum.TryParse<Instances.InstancePrivacyType>(MetaPort.Instance.CurrentInstancePrivacy, out var privacy))
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
                MoveWaitingForGSToQueue(null);
        }
        
        private void MoveWaitingForGSToQueue(RichPresenceInstance_t richPresenceInstanceT)
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
            
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (!TWUtils.IsQMReady()) return;
                
                CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twUserCountUpdate", "Disconnected");
            });

            OnTWNetDisconnected?.Invoke();
        }

        public IEnumerator WaitBeforeSetTargetWorld()
        {
            yield return new WaitForSeconds(10f);

            if (MetaPort.Instance.CurrentInstanceId.Equals(TargetInstanceID)) yield break;
            
            Con.Msg("Set new target world, changing instance!");

            if (_inviteID != null)
            {
                ViewManager.Instance.RespondToInvite(_inviteID, "accept");
                _inviteID = null;
                yield break;
            }
            
            Instances.SetJoinTarget(TargetInstanceID, _targetWorld);
        }

        private void OnAuthWorldCheck()
        {
            if (LastWorldJoinMessage == null) return;

            if (!IsTWNetConnected()) return;

            Send(LastWorldJoinMessage, TWNetMessageTypes.InstanceInfo);
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
            
            Send(userJoin, TWNetMessageTypes.UserJoin);
        }

        private void OnEarlyWorldJoin()
        {
            if (string.IsNullOrWhiteSpace(Patches.Patches.TargetInstanceID))
                return;

            if (_connectedToGS)
                return;

            _connectedToGS = true;

            LastWorldJoinMessage = new InstanceInfo
            {
                InstanceHash = TWUtils.CreateMD5(Patches.Patches.TargetInstanceID)
            };

            if (ConfigManager.Instance.IsActive(AccessType.AllowPetWorldChangeFollow, LeadManager.Instance.MasterId))
                LastWorldJoinMessage.FullInstanceID = Patches.Patches.TargetInstanceID;

            if (!IsTWNetConnected()) return;

            Send(LastWorldJoinMessage, TWNetMessageTypes.InstanceInfo);
        }
    }
}