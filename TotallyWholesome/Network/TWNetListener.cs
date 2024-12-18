﻿using System;
using System.Net;
using MessagePack;
using TotallyWholesome.Managers;
using TotallyWholesome.Managers.Shockers;
using TotallyWholesome.Managers.Status;
using TotallyWholesome.Managers.TWUI;
using TotallyWholesome.Managers.TWUI.Pages;
using TotallyWholesome.Notification;
using TotallyWholesome.Utils;
using TWNetCommon;
using TWNetCommon.Auth;
using TWNetCommon.BasicMessages;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using TWNetCommon.Data.ControlPackets.Shockers;
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
        public static Action<ButtplugUpdate> ButtplugUpdateEvent;
        public static Action<PetConfigUpdate> PetConfigUpdateEvent;
        public static Action<TagData> TagDataUpdateEvent;

        public bool NetworkUnreachable;
        public DateTime ReconnectAttemptTime;

        public override void OnPing(TWNetClient conn)
        {
            //Pong time
            TwTask.Run(TWNetClient.Instance.SendPingAsync());
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
            
            TWMenu.Instance.OnlineUsers = packet.OnlineUsers;
            TWNetClient.Instance.CanUseTag = packet.TagData != null;
            TWNetClient.Instance.CurrentTagData = packet.TagData;

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
                TwTask.Run(TWNetClient.Instance.FollowMaster(arg1.FullInstanceID));
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

        public override void OnShockerControl(ShockerControl update, TWNetClient conn)
        {
            try
            {
                Con.Debug($"[RECV] - {update}");
                TwTask.Run(ShockerManager.Instance.UiControl(update.Type, update.Intensity, update.Duration));
            }
            catch (Exception e)
            {
                Con.Error("An error occured with OnShockerControl");
                Con.Error(e);
            }
        }
        
        public override void OnHeightControl(HeightControl update, TWNetClient conn)
        {
            try
            {
                Con.Debug($"[RECV] - {update}");
                ShockerManager.Instance.Height(update);
            }
            catch (Exception e)
            {
                Con.Error("An error occured with OnShockerControl");
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
                TWMenu.Instance.OnlineUsers = userCount;
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

        public override void OnPetConfigUpdate(PetConfigUpdate update, TWNetClient conn)
        {
            try
            {
                Con.Debug($"[RECV] - {update}");
                PetConfigUpdateEvent?.Invoke(update);
            }
            catch (Exception e)
            {
                Con.Error("An error occured with OnPetConfigUpdate!");
                Con.Error(e);
            }
        }

        public override void OnTagDataUpdate(TagData data, TWNetClient conn)
        {
            try
            {
                Con.Debug($"[RECV] - {data}");

                Main.Instance.MainThreadQueue.Enqueue(() =>
                {
                    TWNetClient.Instance.CanUseTag = data.Success;
                    TWNetClient.Instance.CurrentTagData = data;

                    TagDataUpdateEvent?.Invoke(data);
                });
            }
            catch (Exception e)
            {
                Con.Error("An error occured with OnTagDataUpdate!");
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