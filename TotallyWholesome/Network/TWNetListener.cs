using System;
using System.Net;
using ABI_RC.Core;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.API;
using ABI_RC.Core.Networking.API.UserWebsocket;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Savior;
using cohtml;
using MessagePack;
using TotallyWholesome.Managers;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Status;
using TotallyWholesome.Notification;
using TWNetCommon;
using TWNetCommon.Auth;
using TWNetCommon.BasicMessages;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using WholesomeLoader;

namespace TotallyWholesome.Network
{
    public class TWNetListener : TWPacketHandler<TWNetClient>
    {
        public static Action<Auth> AuthEvent;
        public static Action<LeadAccept> LeadAcceptEvent;
        public static Action<LeadAccept> LeadRemoveEvent;
        public static Action<LeadRequest> MasterRequestEvent;
        public static Action<LeadRequest> PetRequestEvent;
        public static Action<MasterSettings> MasterSettingsEvent;
        public static Action<MasterRemoteControl> MasterRemoteControlEvent;
        public static Action<LeashConfigUpdate> LeashConfigUpdate;
        public static Action<PiShockUpdate> PiShockUpdateEvent;
        public static Action<ButtplugUpdate> ButtplugUpdateEvent;

        public bool NetworkUnreachable;
        public DateTime ReconnectAttemptTime;

        public override void OnPing(TWNetClient conn)
        {
            //Pong time
            TWNetClient.Instance.Send(null, TWNetMessageTypes.PingPong);
        }

        public override void OnAuthResp(AuthResp packet, TWNetClient conn)
        {
            Con.Debug($"[RECV] - {packet}");
            
            if (!packet.Success)
            {
                Con.Error($"Unable to log into TWNet! Reason: \"{packet.RespMsg}\"");
                conn.AuthResponse(false, packet.RespMsg);
                return;
            }

            if (packet.UpdateLoader && !Environment.CommandLine.Contains("--TWParanoidMode"))
            {
                Con.Msg("WholesomeLoader is outdated! Updating...");
                
                using (var web = new WebClient())
                {
                    web.Headers.Add("User-Agent", "TotallyWholesome");
                    web.DownloadFile("http://aurares.potato.moe/WholesomeLoader.dll", TWNetClient.Instance.WholesomeLoaderLocation);
                }
                
                Con.Msg("WholesomeLoader updated! When you restart the new version will be used!");
            }

            conn.OnlineUsers = packet.OnlineUsers;
            
            Main.Instance.MainThreadQueue.Enqueue(() =>
            { 
                if (!TWUtils.IsQMReady()) return;

                CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twUserCountUpdate", conn.OnlineUsers);
            });

            conn.AuthResponse(true, packet.RespMsg);
        }

        public override void OnDisconnectMessage(MessageResponse arg1, TWNetClient conn)
        {
            Con.Debug($"[RECV] - {arg1}");
            TWNetClient.Instance.DisconnectMessage = arg1;
        }

        public override void OnLeadAccept(LeadAccept packet, TWNetClient conn)
        {
            try
            {
                Con.Debug($"[RECV] - {packet}");
                
                if (packet.LeadRemove)
                {
                    LeadRemoveEvent?.Invoke(packet);
                    return;
                }
                
                LeadAcceptEvent?.Invoke(packet);
            }
            catch (Exception e)
            {
                Con.Error("An error occured during OnLeadAccept!");
                Con.Error(e);
            }
        }

        public override void OnLeadRequestResponse(MessageResponse packet, TWNetClient conn)
        {
            if (string.IsNullOrWhiteSpace(packet.Message)) return;
            
            NotificationSystem.EnqueueNotification("Totally Wholesome", packet.Message, 5f, TWAssets.Alert);
            Con.Warn("Lead Request failed! Response Message: " + packet.Message);

        }

        public override void OnLeadRequest(LeadRequest packet, TWNetClient conn)
        {
            try
            {
                if(!packet.MasterRequest)
                    MasterRequestEvent?.Invoke(packet);
                else
                    PetRequestEvent?.Invoke(packet);    
            }
            catch (Exception e)
            {
                Con.Error("An error occured during OnLeadRequest!");
                Con.Error(e);
            }
        }

        public override void OnMasterSettings(MasterSettings packet, TWNetClient conn)
        {
            try
            {
                MasterSettingsEvent?.Invoke(packet);
            }
            catch (Exception e)
            {
                Con.Error("An error occured during OnMasterSettings!");
                Con.Error(e);
            }
        }

        public override void OnInstanceInfo(InstanceInfo arg1, TWNetClient conn)
        {
            try
            {
                TWNetClient.Instance.FollowMaster(arg1.FullInstanceID);
            }
            catch (Exception e)
            {
                Con.Error("An error occured during OnInstanceInfo!");
                Con.Error(e);
            }
        }

        public override void OnStatusUpdate(StatusUpdate packet, TWNetClient conn)
        {
            try
            {
                Con.Debug($"[RECV] - {packet}");
                StatusManager.Instance.OnStatusUpdate(packet);
            }
            catch (Exception e)
            {
                Con.Error("An error occured during OnStatusUpdate!");
                Con.Error(e);
            }
        }

        public override void OnSerializationException(MessagePackSerializationException exception, int packetID)
        {
            Con.Error($"A serialization exception was triggered in packet - {packetID} - PLEASE REPORT THIS LOG IN BETA BUG REPORTS!");
            Con.Error(exception);
        }

        public override void OnMasterRemoteControl(MasterRemoteControl packet, TWNetClient conn)
        {
            try
            {
                Con.Debug($"[RECV] - {packet}");
                MasterRemoteControlEvent?.Invoke(packet);
            }
            catch (Exception e)
            {
                Con.Error("An error occured during OnMasterRemoteControl");
                Con.Error(e);
            }
        }
        
        public override void OnButtplugUpdate(ButtplugUpdate packet, TWNetClient conn)
        {
            try
            {
                Con.Debug($"[RECV] - {packet}");
                ButtplugUpdateEvent?.Invoke(packet);
            }
            catch (Exception e)
            {
                Con.Error("An error occured during OnButtplugUpdate");
                Con.Error(e);
            }
        }

        public override void OnPiShockUpdate(PiShockUpdate packet, TWNetClient conn)
        {
            try
            {
                Con.Debug($"[RECV] - {packet}");
                PiShockUpdateEvent?.Invoke(packet);
            }
            catch (Exception e)
            {
                Con.Error("An error occured during OnPiShockUpdate");
                Con.Error(e);
            }
        }

        public override void OnSystemNotice(MessageResponse packet, TWNetClient conn)
        {
            if (packet.Message == null) return;

            conn.HasBeenRatelimited = packet.Message.Contains("You are being ratelimited!");
            
            Con.Msg($"System Notice - {packet.Message}");
            NotificationSystem.EnqueueNotification("TWNet Notice", packet.Message, 10f, TWAssets.Megaphone);
        }

        public override void OnUserCountUpdated(MessageResponse packet, TWNetClient conn)
        {
            int userCount = 0;
            if (int.TryParse(packet.Message, out userCount))
            {
                TWNetClient.Instance.OnlineUsers = userCount;
                
                Main.Instance.MainThreadQueue.Enqueue(() =>
                { 
                    if (!TWUtils.IsQMReady()) return;
                
                    CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twUserCountUpdate", userCount.ToString());
                });
            }
        }

        public override void OnPairJoinNotification(PairJoinNotification packet, TWNetClient conn)
        {
            if (!ConfigManager.Instance.IsActive(AccessType.MasterPetJoinNotification) ) return;

            if (string.IsNullOrWhiteSpace(packet.DisplayName)) return;

            NotificationSystem.EnqueueNotification(packet.Master?"Master Joined":"Pet Joined", $"{packet.DisplayName} has joined your instance!", 3f, TWAssets.Key);
        }

        public override void OnInstanceFollowResponse(InstanceFollowResponse packet, TWNetClient conn)
        {
            TWNetClient.Instance.OnInstanceFollowResponse(packet);
        }

        public override void OnStatusUpdateConfirmation(StatusUpdateConfirmation packet, TWNetClient conn)
        {
            base.OnStatusUpdateConfirmation(packet, conn);
        }

        public override void OnLeashConfigUpdate(LeashConfigUpdate update, TWNetClient conn)
        {
            try
            {
                Con.Debug($"[RECV] - {update}");
                LeashConfigUpdate?.Invoke(update);
            }
            catch (Exception e)
            {
                Con.Error("An error occured with OnLeashConfigUpdate!");
                Con.Error(e);
            }
        }

        public override void OnPacketHandlerException(Exception exception, int packetID)
        {
            Con.Error($"An exception occured within Packet Handler! Packet ID:{packetID} - PLEASE REPORT THIS LOG IN BETA BUG REPORTS!");
            Con.Error(exception);
        }
    }
}