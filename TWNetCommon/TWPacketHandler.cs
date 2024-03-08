using System;
using System.Collections.Generic;
using System.Threading;
using MessagePack;
using TWNetCommon.Auth;
using TWNetCommon.BasicMessages;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using TWNetCommon.Data.ControlPackets.Shockers;

namespace TWNetCommon
{
    public class TWPacketHandler<TConnection>
    {
        public void HandlePacket(TConnection conn, byte[] bytes, int packetID)
        {
            int bytesRead = 0;

            try
            {
                switch ((TWNetMessageType)packetID)
                {
                    case TWNetMessageType.PingPong:
                        OnPing(conn);
                        break;
                    case TWNetMessageType.Auth:
                        var authData = MessagePackSerializer.Deserialize<Auth.Auth>(bytes, out bytesRead);
                        OnAuth(authData, conn);
                        break;
                    case TWNetMessageType.AuthResp:
                        var authResp = MessagePackSerializer.Deserialize<AuthResp>(bytes, out bytesRead);
                        OnAuthResp(authResp, conn);
                        break;
                    case TWNetMessageType.Disconnection:
                        var disconnection = MessagePackSerializer.Deserialize<MessageResponse>(bytes, out bytesRead);
                        OnDisconnectMessage(disconnection, conn);
                        break;
                    case TWNetMessageType.SystemNotice:
                        var systemNotice = MessagePackSerializer.Deserialize<MessageResponse>(bytes, out bytesRead);
                        OnSystemNotice(systemNotice, conn);
                        break;
                    case TWNetMessageType.UserCountUpdated:
                        var userCount = MessagePackSerializer.Deserialize<MessageResponse>(bytes, out bytesRead);
                        OnUserCountUpdated(userCount, conn);
                        break;
                    case TWNetMessageType.PairJoinNotification:
                        var pairJoin = MessagePackSerializer.Deserialize<PairJoinNotification>(bytes, out bytesRead);
                        OnPairJoinNotification(pairJoin, conn);
                        break;
                    case TWNetMessageType.LeadRequest:
                        var leadReq = MessagePackSerializer.Deserialize<LeadRequest>(bytes, out bytesRead);
                        OnLeadRequest(leadReq, conn);
                        break;
                    case TWNetMessageType.LeadRequestResp:
                        var leadReqResp = MessagePackSerializer.Deserialize<MessageResponse>(bytes, out bytesRead);
                        OnLeadRequestResponse(leadReqResp, conn);
                        break;
                    case TWNetMessageType.LeadAccept:
                        var leadAccept = MessagePackSerializer.Deserialize<LeadAccept>(bytes, out bytesRead);
                        OnLeadAccept(leadAccept, conn);
                        break;
                    case TWNetMessageType.InstanceInfo:
                        var instanceInfo = MessagePackSerializer.Deserialize<InstanceInfo>(bytes, out bytesRead);
                        OnInstanceInfo(instanceInfo, conn);
                        break;
                    case TWNetMessageType.MasterRemoteControl3:
                        var masterRemote = MessagePackSerializer.Deserialize<MasterRemoteControl>(bytes, out bytesRead);
                        OnMasterRemoteControl(masterRemote, conn);
                        break;
                    case TWNetMessageType.MasterSettings:
                        var masterSettings = MessagePackSerializer.Deserialize<MasterSettings>(bytes, out bytesRead);
                        OnMasterSettings(masterSettings, conn);
                        break;
                    case TWNetMessageType.StatusUpdate:
                        var status = MessagePackSerializer.Deserialize<StatusUpdate>(bytes, out bytesRead);
                        OnStatusUpdate(status, conn);
                        break;
                    case TWNetMessageType.StatusUpdateConfirmation:
                        var statusConfirm = MessagePackSerializer.Deserialize<StatusUpdateConfirmation>(bytes, out bytesRead);
                        OnStatusUpdateConfirmation(statusConfirm, conn);
                        break;
                    case TWNetMessageType.UserJoin:
                        var userJoin = MessagePackSerializer.Deserialize<UserInstanceChange>(bytes, out bytesRead);
                        OnUserJoin(userJoin, conn);
                        break;
                    case TWNetMessageType.UserLeave:
                        var userLeave = MessagePackSerializer.Deserialize<UserInstanceChange>(bytes, out bytesRead);
                        OnUserLeave(userLeave, conn);
                        break;

                    case TWNetMessageType.InstanceFollowResponse:
                        var response = MessagePackSerializer.Deserialize<InstanceFollowResponse>(bytes, out bytesRead);
                        OnInstanceFollowResponse(response, conn);
                        break;
                    case TWNetMessageType.LeashConfigUpdate:
                        var config = MessagePackSerializer.Deserialize<LeashConfigUpdate>(bytes, out bytesRead);
                        OnLeashConfigUpdate(config, conn);
                        break;
                    case TWNetMessageType.PiShockUpdate:
                        var psu = MessagePackSerializer.Deserialize<PiShockUpdate>(bytes, out bytesRead);
                        OnPiShockUpdate(psu, conn);
                        break;
                    case TWNetMessageType.ButtplugUpdate:
                        var bpu = MessagePackSerializer.Deserialize<ButtplugUpdate>(bytes, out bytesRead);
                        OnButtplugUpdate(bpu, conn);
                        break;
                    case TWNetMessageType.PetConfigUpdate:
                        var pcu = MessagePackSerializer.Deserialize<PetConfigUpdate>(bytes, out bytesRead);
                        OnPetConfigUpdate(pcu, conn);
                        break;

                    case TWNetMessageType.ShockerControl:
                        var shockerControl = MessagePackSerializer.Deserialize<ShockerControl>(bytes, out bytesRead);
                        OnShockerControl(shockerControl, conn);
                        break;
                    case TWNetMessageType.HeightControl:
                        var shockerHeight = MessagePackSerializer.Deserialize<HeightControl>(bytes, out bytesRead);
                        OnHeightControl(shockerHeight, conn);
                        break;

                    default:
                        throw new Exception("Packet not registered in TWPacketHandler!");
                }
            }
            catch (MessagePackSerializationException e)
            {
                OnSerializationException(e, packetID);
            }
            catch (Exception e)
            {
                OnPacketHandlerException(e, packetID);
            }

            if (bytes.Length > bytesRead)
            {
                OnByteLengthMismatch(conn, bytesRead, bytes.Length);
            }
        }
        
        public virtual void OnAuth(Auth.Auth packet, TConnection conn) {}
        public virtual void OnAuthResp(AuthResp packet, TConnection conn) {}
        public virtual void OnDisconnectMessage(MessageResponse packet, TConnection conn) {}
        public virtual void OnSystemNotice(MessageResponse packet, TConnection conn) {}
        public virtual void OnUserCountUpdated(MessageResponse packet, TConnection conn) {}
        public virtual void OnPairJoinNotification(PairJoinNotification packet, TConnection conn) {}
        public virtual void OnLeadRequest(LeadRequest packet, TConnection conn) {}
        public virtual void OnLeadRequestResponse(MessageResponse packet, TConnection conn) {}
        public virtual void OnLeadAccept(LeadAccept packet, TConnection conn) {}
        public virtual void OnInstanceInfo(InstanceInfo packet, TConnection conn) {}
        public virtual void OnMasterRemoteControl(MasterRemoteControl packet, TConnection conn) {}
        public virtual void OnMasterSettings(MasterSettings packet, TConnection conn) {}
        public virtual void OnStatusUpdate(StatusUpdate packet, TConnection conn) {}
        public virtual void OnStatusUpdateConfirmation(StatusUpdateConfirmation packet, TConnection conn) {}
        public virtual void OnPing(TConnection conn) {}
        public virtual void OnUserJoin(UserInstanceChange packet, TConnection conn) {}
        public virtual void OnUserLeave(UserInstanceChange packet, TConnection conn) {}

        public virtual void OnInstanceFollowResponse(InstanceFollowResponse packet, TConnection conn) { }
        public virtual void OnSerializationException(MessagePackSerializationException exception, int packetID) { }
        public virtual void OnPacketHandlerException(Exception exception, int packetID) {}
        public virtual void OnLeashConfigUpdate(LeashConfigUpdate update, TConnection conn) { }
        public virtual void OnButtplugUpdate(ButtplugUpdate update, TConnection conn) { }
        public virtual void OnPiShockUpdate(PiShockUpdate update, TConnection conn) { }
        public virtual void OnShockerControl(ShockerControl update, TConnection conn) { }
        public virtual void OnHeightControl(HeightControl update, TConnection conn) { }
        public virtual void OnPetConfigUpdate(PetConfigUpdate update, TConnection conn){ }
        public virtual void OnByteLengthMismatch(TConnection conn, int readBytes, int totalBytes) { }
    }
}