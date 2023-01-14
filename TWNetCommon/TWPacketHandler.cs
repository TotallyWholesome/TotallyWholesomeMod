using System;
using System.Collections.Generic;
using System.Threading;
using MessagePack;
using TWNetCommon.Auth;
using TWNetCommon.BasicMessages;
using TWNetCommon.Data;

namespace TWNetCommon
{
    public class TWPacketHandler<TConnection>
    {
        public void HandlePacket(TConnection conn, byte[] bytes, int packetID, Action<byte[], int> receiveCallback = null)
        {
            int bytesRead = 0;

            try
            {
                switch (packetID)
                {
                    case TWNetMessageTypes.PingPong:
                        OnPing(conn);
                        break;
                    case TWNetMessageTypes.Auth:
                        var authData = MessagePackSerializer.Deserialize<Auth.Auth>(bytes, out bytesRead);
                        OnAuth(authData, conn);
                        break;
                    case TWNetMessageTypes.AuthResp:
                        var authResp = MessagePackSerializer.Deserialize<AuthResp>(bytes, out bytesRead);
                        OnAuthResp(authResp, conn);
                        break;
                    case TWNetMessageTypes.Disconnection:
                        var disconnection = MessagePackSerializer.Deserialize<MessageResponse>(bytes, out bytesRead);
                        OnDisconnectMessage(disconnection, conn);
                        break;
                    case TWNetMessageTypes.SystemNotice:
                        var systemNotice = MessagePackSerializer.Deserialize<MessageResponse>(bytes, out bytesRead);
                        OnSystemNotice(systemNotice, conn);
                        break;
                    case TWNetMessageTypes.UserCountUpdated:
                        var userCount = MessagePackSerializer.Deserialize<MessageResponse>(bytes, out bytesRead);
                        OnUserCountUpdated(userCount, conn);
                        break;
                    case TWNetMessageTypes.PairJoinNotification:
                        var pairJoin = MessagePackSerializer.Deserialize<PairJoinNotification>(bytes, out bytesRead);
                        OnPairJoinNotification(pairJoin, conn);
                        break;
                    case TWNetMessageTypes.LeadRequest:
                        var leadReq = MessagePackSerializer.Deserialize<LeadRequest>(bytes, out bytesRead);
                        OnLeadRequest(leadReq, conn);
                        break;
                    case TWNetMessageTypes.LeadRequestResp:
                        var leadReqResp = MessagePackSerializer.Deserialize<MessageResponse>(bytes, out bytesRead);
                        OnLeadRequestResponse(leadReqResp, conn);
                        break;
                    case TWNetMessageTypes.LeadAccept:
                        var leadAccept = MessagePackSerializer.Deserialize<LeadAccept>(bytes, out bytesRead);
                        OnLeadAccept(leadAccept, conn);
                        break;
                    case TWNetMessageTypes.InstanceInfo:
                        var instanceInfo = MessagePackSerializer.Deserialize<InstanceInfo>(bytes, out bytesRead);
                        OnInstanceInfo(instanceInfo, conn);
                        break;
                    case TWNetMessageTypes.MasterRemoteControl:
                        var masterRemote = MessagePackSerializer.Deserialize<MasterRemoteControl>(bytes, out bytesRead);
                        OnMasterRemoteControl(masterRemote, conn);
                        break;
                    case TWNetMessageTypes.MasterSettings:
                        var masterSettings = MessagePackSerializer.Deserialize<MasterSettings>(bytes, out bytesRead);
                        OnMasterSettings(masterSettings, conn);
                        break;
                    case TWNetMessageTypes.StatusUpdate:
                        var status = MessagePackSerializer.Deserialize<StatusUpdate>(bytes, out bytesRead);
                        OnStatusUpdate(status, conn);
                        break;
                    case TWNetMessageTypes.StatusUpdateConfirmation:
                        var statusConfirm = MessagePackSerializer.Deserialize<StatusUpdateConfirmation>(bytes, out bytesRead);
                        OnStatusUpdateConfirmation(statusConfirm, conn);
                        break;
                    case TWNetMessageTypes.UserJoin:
                        var userJoin = MessagePackSerializer.Deserialize<UserInstanceChange>(bytes, out bytesRead);
                        OnUserJoin(userJoin, conn);
                        break;
                    case TWNetMessageTypes.UserLeave:
                        var userLeave = MessagePackSerializer.Deserialize<UserInstanceChange>(bytes, out bytesRead);
                        OnUserLeave(userLeave, conn);
                        break;

                    case TWNetMessageTypes.InstanceFollowResponse:
                        var response = MessagePackSerializer.Deserialize<InstanceFollowResponse>(bytes, out bytesRead);
                        OnInstanceFollowResponse(response, conn);
                        break;
                    case TWNetMessageTypes.LeashConfigUpdate:
                        var config = MessagePackSerializer.Deserialize<LeashConfigUpdate>(bytes, out bytesRead);
                        OnLeashConfigUpdate(config, conn);
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
                //We have multiple packets in buffer, return to receive
                byte[] newArray = new byte[bytes.Length - bytesRead];
                Array.Copy(bytes, bytesRead, newArray, 0, newArray.Length);

                receiveCallback?.Invoke(newArray, newArray.Length);
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
    }
}