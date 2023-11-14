using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TotallyWholesome.Managers;
using TotallyWholesome.Managers.Lead;
using TWNetCommon;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using TWNetCommon.Data.NestedObjects;
using UnityEngine;

namespace TotallyWholesome.Network
{
    public static class TWNetSendHelpers
    {
        private static DateTime _lastRemoteSettingsUpdate;
        private static DateTime _lastMasterSettingsUpdate;
        private static DateTime _lastLeashConfigUpdate;
        private static DateTime _lastPiShockUpdate;
        private static DateTime _lastButtplugUpdate;
        private static Task _remoteSettingsTask;
        private static Task _masterSettingsTask;
        private static Task _leashConfigUpdate;
        private static Task _piShockUpdate;
        private static Task _buttplugUpdate;

        public static void UpdateMasterSettingsAsync(LeadPair pair = null)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return;

            if (_masterSettingsTask != null && !_masterSettingsTask.IsCompleted)
                return;

            _masterSettingsTask = Task.Run(() =>
            {
                var timeBetweenLast = DateTime.Now.Subtract(_lastMasterSettingsUpdate).Milliseconds;

                //Ensure MasterRemoteSettings waits before being sent
                if (timeBetweenLast <= 50)
                    Thread.Sleep(50 - timeBetweenLast);

                _lastMasterSettingsUpdate = DateTime.Now;

                MasterSettings packet = new MasterSettings();

                if (pair != null)
                {
                    //Individual pet controls
                    packet.Key = pair.Key;
                    packet.LeadLength = pair.LeadLength;
                    packet.TempUnlockLeash = pair.TempUnlockLeash;
                }
                else
                {
                    //Global pet controls
                    packet.LeadLength = LeadManager.Instance.TetherRange.SliderValue;
                    packet.TempUnlockLeash = LeadManager.Instance.TempUnlockLeash;

                    //Update our pairs with the global data
                    foreach (var keyPair in LeadManager.Instance.ActiveLeadPairs.Where(x => x.Value.AreWeMaster()))
                    {
                        keyPair.Value.LeadLength = LeadManager.Instance.TetherRange.SliderValue;
                        keyPair.Value.TempUnlockLeash = LeadManager.Instance.TempUnlockLeash;
                        keyPair.Value.GlobalValuesUpdate = true;
                    }
                }

                TWNetClient.Instance.Send(packet, TWNetMessageTypes.MasterSettings);
            });
        }
        
        public static void SendPiShockUpdate(LeadPair pair = null)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return;

            if (_piShockUpdate != null && !_piShockUpdate.IsCompleted)
                return;

            _piShockUpdate = Task.Run(() =>
            {
                var timeBetweenLast = DateTime.Now.Subtract(_lastPiShockUpdate).Milliseconds;

                //Ensure MasterRemoteSettings waits before being sent
                if (timeBetweenLast <= 100)
                    Thread.Sleep(100 - timeBetweenLast);
                
                _lastPiShockUpdate = DateTime.Now;

                PiShockUpdate packet = new PiShockUpdate();

                if (pair != null)
                {
                    packet.Key = pair.Key;
                    packet.ShockOperation = pair.ShockOperation;
                    packet.ShockDuration = pair.ShockDuration;
                    packet.ShockDurationMillis = (uint)(PiShockManager.Instance.Duration.SliderValue * 1000);
                    packet.ShockStrength = pair.ShockStrength;
                    packet.ShockHeightEnabled = pair.ShockHeightEnabled;
                    packet.ShockHeight = pair.ShockHeight;
                    packet.ShockHeightStrengthMin = pair.ShockHeightStrengthMin;
                    packet.ShockHeightStrengthMax = pair.ShockHeightStrengthMax;
                    packet.ShockHeightStrengthStep = pair.ShockHeightStrengthStep;
                    pair.ShockOperation = ShockOperation.NoOp;

                    PiShockManager.Instance.LastStrengthMaster = packet.ShockStrength;
                    PiShockManager.Instance.LastDurationMaster = packet.ShockDuration;
                    PiShockManager.Instance.LastOperationMaster = packet.ShockOperation;
                    PiShockManager.Instance.LastOperationFiredMaster = DateTime.Now;
                }
                else
                {
                    packet.ShockDuration = (int)Math.Ceiling(PiShockManager.Instance.Duration.SliderValue);
                    packet.ShockDurationMillis = (uint)(PiShockManager.Instance.Duration.SliderValue * 1000);
                    packet.ShockStrength = (int)Math.Ceiling(PiShockManager.Instance.Strength.SliderValue);
                    packet.ShockOperation = PiShockManager.Instance.Operation;
                    packet.ShockHeightEnabled = PiShockManager.Instance.ShockHeightEnabled;
                    packet.ShockHeight = PiShockManager.Instance.ShockHeight.SliderValue;
                    packet.ShockHeightStrengthMin = PiShockManager.Instance.ShockHeightStrengthMin.SliderValue;
                    packet.ShockHeightStrengthMax = PiShockManager.Instance.ShockHeightStrengthMax.SliderValue;
                    packet.ShockHeightStrengthStep = PiShockManager.Instance.ShockHeightStrengthStep.SliderValue;
                    PiShockManager.Instance.Operation = ShockOperation.NoOp;
                    
                    PiShockManager.Instance.LastStrengthMaster = packet.ShockStrength;
                    PiShockManager.Instance.LastDurationMaster = packet.ShockDuration;
                    PiShockManager.Instance.LastOperationMaster = packet.ShockOperation;
                    PiShockManager.Instance.LastOperationFiredMaster = DateTime.Now;
                    PiShockManager.Instance.LastOperationGlobalMaster = DateTime.Now;

                    foreach (var keyPair in LeadManager.Instance.ActiveLeadPairs.Where(x => x.Value.AreWeMaster()))
                    {
                        keyPair.Value.ShockDuration = packet.ShockDuration;
                        keyPair.Value.ShockDurationMillis = packet.ShockDurationMillis;
                        keyPair.Value.ShockStrength = packet.ShockStrength;
                        keyPair.Value.ShockOperation = packet.ShockOperation;
                        keyPair.Value.ShockHeightEnabled = packet.ShockHeightEnabled;
                        keyPair.Value.ShockHeight = packet.ShockHeight;
                        keyPair.Value.ShockHeightStrengthMin = packet.ShockHeightStrengthMin;
                        keyPair.Value.ShockHeightStrengthMax = packet.ShockHeightStrengthMax;
                        keyPair.Value.ShockHeightStrengthStep = packet.ShockHeightStrengthStep;
                        keyPair.Value.GlobalValuesUpdate = true;
                    }
                }
                
                TWNetClient.Instance.Send(packet, TWNetMessageTypes.PiShockUpdate);
            });
        }

        public static void SendButtplugUpdate(LeadPair pair = null)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return;

            if (_buttplugUpdate != null && !_buttplugUpdate.IsCompleted)
                return;

            _buttplugUpdate = Task.Run(() =>
            {
                var timeBetweenLast = DateTime.Now.Subtract(_lastButtplugUpdate).Milliseconds;

                //Ensure MasterRemoteSettings waits before being sent
                if (timeBetweenLast <= 100)
                    Thread.Sleep(100 - timeBetweenLast);

                _lastButtplugUpdate = DateTime.Now;

                var packet = new ButtplugUpdate();

                if (pair != null)
                {
                    packet.Key = pair.Key;
                    packet.ToyStrength = pair.ToyStrength;
                }
                else
                {
                    packet.ToyStrength = ButtplugManager.Instance.ToyStrength.SliderValue;
                    
                    //Update our pairs with the global settings
                    foreach (var keyPair in LeadManager.Instance.ActiveLeadPairs.Where(x => x.Value.AreWeMaster()))
                    {
                        keyPair.Value.ToyStrength = ButtplugManager.Instance.ToyStrength.SliderValue;
                        keyPair.Value.GlobalValuesUpdate = true;
                    }
                }
                
                TWNetClient.Instance.Send(packet, TWNetMessageTypes.ButtplugUpdate);
            });
        }

        public static void SendMasterRemoteSettingsAsync(LeadPair pair = null)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return;

            if (_remoteSettingsTask != null && !_remoteSettingsTask.IsCompleted)
                return;

            _remoteSettingsTask = Task.Run(() =>
            {
                var timeBetweenLast = DateTime.Now.Subtract(_lastRemoteSettingsUpdate).Milliseconds;

                //Ensure MasterRemoteSettings waits before being sent
                if (timeBetweenLast <= 100)
                    Thread.Sleep(100 - timeBetweenLast);

                _lastRemoteSettingsUpdate = DateTime.Now;

                MasterRemoteControl packet = new MasterRemoteControl();

                if (pair != null)
                {
                    packet.Key = pair.Key;
                    packet.GagPet = pair.ForcedMute;
                    packet.Parameters = new List<MasterRemoteParameter>();
                    packet.DisableFlight = pair.DisableFlight;
                    packet.DisableSeats = pair.DisableSeats;
                    packet.BlindPet = pair.Blindfold;
                    packet.DeafenPet = pair.Deafen;
                    
                    if (pair.PropTarget != null)
                        packet.PropTarget = pair.LockToProp ? pair.PropTarget : null;

                    if (pair.LeashPinPosition != Vector3.zero)
                        packet.LeashPinPosition = pair.LockToWorld ? pair.LeashPinPosition.ToTWVector3() : TWVector3.Zero;

                    pair.ShockOperation = ShockOperation.NoOp;

                    var paramsUpdated = pair.PetEnabledParameters.Where(x => x.IsUpdated).ToList();

                    if (paramsUpdated.Count > 0)
                    {
                        pair.PetEnabledParameters.ForEach(x => x.IsUpdated = false);
                        packet.Parameters = paramsUpdated;
                    }
                }
                else
                {
                    packet.GagPet = LeadManager.Instance.ForcedMute;
                    packet.DisableFlight = LeadManager.Instance.DisableFlight;
                    packet.DisableSeats = LeadManager.Instance.DisableSeats;
                    packet.BlindPet = LeadManager.Instance.Blindfold;
                    packet.DeafenPet = LeadManager.Instance.Deafen;

                    if (LeadManager.Instance.PropTarget != null)
                        packet.PropTarget = LeadManager.Instance.LockToProp ? LeadManager.Instance.PropTarget : null;

                    if (LeadManager.Instance.LeashPinPosition != Vector3.zero)
                        packet.LeashPinPosition = LeadManager.Instance.LockToWorld ? LeadManager.Instance.LeashPinPosition.ToTWVector3() : TWVector3.Zero;

                    //Update our pairs with the global settings
                    foreach (var keyPair in LeadManager.Instance.ActiveLeadPairs.Where(x => x.Value.AreWeMaster()))
                    {
                        keyPair.Value.ForcedMute = LeadManager.Instance.ForcedMute;
                        keyPair.Value.DisableFlight = LeadManager.Instance.DisableFlight;
                        keyPair.Value.DisableSeats = LeadManager.Instance.DisableSeats;
                        keyPair.Value.PropTarget = LeadManager.Instance.PropTarget;
                        keyPair.Value.LeashPinPosition = LeadManager.Instance.LeashPinPosition;
                        keyPair.Value.LockToProp = LeadManager.Instance.LockToProp;
                        keyPair.Value.LockToWorld = LeadManager.Instance.LockToWorld;
                        keyPair.Value.Blindfold = LeadManager.Instance.Blindfold;
                        keyPair.Value.Deafen = LeadManager.Instance.Deafen;
                        keyPair.Value.GlobalValuesUpdate = true;
                    }
                }

                TWNetClient.Instance.Send(packet, TWNetMessageTypes.MasterRemoteControl2);
            });
        }

        public static void AcceptPetRequest(string key, string requesterID)
        {
            LeadManager.Instance.FollowerRequest = true;
            LeadManager.Instance.LastKey = key;

            LeadAccept packet = new LeadAccept()
            {
                MasterID = requesterID,
                PetBoneTarget = (int)Configuration.JSONConfig.PetBoneTarget,
                NoVisibleLeash = ConfigManager.Instance.IsActive(AccessType.NoVisibleLeash, requesterID),
                PrivateLeash = ConfigManager.Instance.IsActive(AccessType.PrivateLeash, requesterID),
                PetLeashColour = ConfigManager.Instance.IsActive(AccessType.UseCustomLeashColour) ? Configuration.JSONConfig.LeashColour : "",
                Key = key,
                FollowerAccept = true
            };

            TWNetClient.Instance.Send(packet, TWNetMessageTypes.LeadAccept);
        }

        public static void AcceptMasterRequest(string key, string requesterID)
        {
            LeadManager.Instance.FollowerRequest = false;
            LeadManager.Instance.LastKey = key;

            LeadAccept packet = new LeadAccept()
            {
                FollowerID = requesterID,
                MasterBoneTarget = (int)Configuration.JSONConfig.MasterBoneTarget,
                LeadLength = LeadManager.Instance.TetherRange.SliderValue,
                NoVisibleLeash = ConfigManager.Instance.IsActive(AccessType.NoVisibleLeash, requesterID),
                PrivateLeash = ConfigManager.Instance.IsActive(AccessType.PrivateLeash, requesterID),
                MasterLeashColour = ConfigManager.Instance.IsActive(AccessType.UseCustomLeashColour) ? Configuration.JSONConfig.LeashColour : "",
                LeashStyle = (int)Configuration.JSONConfig.LeashStyle,
                DisableFlight = LeadManager.Instance.DisableFlight,
                DisableSeats = LeadManager.Instance.DisableSeats,
                TempUnlockLeash = LeadManager.Instance.TempUnlockLeash,
                Key = key
            };

            TWNetClient.Instance.Send(packet, TWNetMessageTypes.LeadAccept);
        }

        public static void SendLeashConfigUpdate(LeadPair pair = null)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return;

            if (_leashConfigUpdate != null && !_leashConfigUpdate.IsCompleted)
                return;

            _leashConfigUpdate = Task.Run(() =>
            {
                var timeBetweenLast = DateTime.Now.Subtract(_lastLeashConfigUpdate).Milliseconds;

                //Ensure MasterRemoteSettings waits before being sent
                if (timeBetweenLast <= 100)
                    Thread.Sleep(100 - timeBetweenLast);

                _lastLeashConfigUpdate = DateTime.Now;

                var packet = new LeashConfigUpdate()
                {
                    MasterLeashColour = ConfigManager.Instance.IsActive(AccessType.UseCustomLeashColour) ? Configuration.JSONConfig.LeashColour : "",
                    PetLeashColour = ConfigManager.Instance.IsActive(AccessType.UseCustomLeashColour) ? Configuration.JSONConfig.LeashColour : "",
                    LeashStyle = (int)Configuration.JSONConfig.LeashStyle
                };

                if (pair != null)
                    packet.Key = pair.Key;

                if (pair?.PropTarget != null)
                    packet.PropTarget = pair.LockToProp ? pair.PropTarget : null;

                if (pair == null && LeadManager.Instance.PropTarget != null)
                    packet.PropTarget = LeadManager.Instance.LockToProp ? LeadManager.Instance.PropTarget : null;

                if (pair != null && pair.LeashPinPosition != Vector3.zero)
                    packet.LeashPinPosition = pair.LockToWorld ? pair.LeashPinPosition.ToTWVector3() : TWVector3.Zero;

                if (pair == null && LeadManager.Instance.LeashPinPosition != Vector3.zero)
                    packet.LeashPinPosition = LeadManager.Instance.LockToWorld ? LeadManager.Instance.LeashPinPosition.ToTWVector3() : TWVector3.Zero;

                if (pair == null)
                {
                    foreach (var keyPair in LeadManager.Instance.ActiveLeadPairs.Where(x => x.Value.AreWeMaster()))
                    {
                        keyPair.Value.PropTarget = LeadManager.Instance.PropTarget;
                        keyPair.Value.LeashPinPosition = LeadManager.Instance.LeashPinPosition;
                        keyPair.Value.LockToProp = LeadManager.Instance.LockToProp;
                        keyPair.Value.LockToWorld = LeadManager.Instance.LockToWorld;
                    }
                }

                TWNetClient.Instance.Send(packet, TWNetMessageTypes.LeashConfigUpdate);
            });
        }
    }
}