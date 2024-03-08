#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TotallyWholesome.Managers;
using TotallyWholesome.Managers.AvatarParams;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Shockers;
using TotallyWholesome.Utils;
using TWNetCommon;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using TWNetCommon.Data.ControlPackets.Shockers;
using TWNetCommon.Data.ControlPackets.Shockers.Models;
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
        private static DateTime _lastPetConfigUpdate;
        private static Task? _remoteSettingsTask;
        private static Task? _masterSettingsTask;
        private static Task? _leashConfigUpdate;
        private static Task? _piShockUpdate;
        private static Task? _buttplugUpdate;
        private static Task? _petConfigUpdate;

        public static void UpdateMasterSettingsAsync(LeadPair? pair = null)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return;

            if (_masterSettingsTask != null && !_masterSettingsTask.IsCompleted)
                return;

            _masterSettingsTask = TwTask.Run(async () =>
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

                await TWNetClient.Instance.SendAsync(packet, TWNetMessageType.MasterSettings);
            });
        }

        /// <summary>
        /// Control a shocker
        /// </summary>
        /// <param name="type"></param>
        /// <param name="intensity"></param>
        /// <param name="duration"></param>
        /// <param name="petPair"></param>
        /// <returns></returns>
        public static Task SendShockControl(
            ControlType type,
            byte intensity,
            ushort duration,
            LeadPair? petPair = null)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return Task.CompletedTask;
            
            var networkPacket = new ShockerControl
            {
                Key = petPair?.Key,
                Type = type,
                Intensity = intensity,
                Duration = duration
            };

            return TWNetClient.Instance.SendAsync(networkPacket, TWNetMessageType.ShockerControl);
        }

        /// <summary>
        /// Height control shocking
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="height"></param>
        /// <param name="strengthMin"></param>
        /// <param name="strengthMax"></param>
        /// <param name="strengthStep"></param>
        /// <param name="petPair"></param>
        /// <returns></returns>
        public static Task SendHeightControl(
            bool enabled,
            float height,
            float strengthMin,
            float strengthMax,
            float strengthStep,
            LeadPair? petPair = null)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return Task.CompletedTask;
            
            var networkPacket = new HeightControl
            {
                Key = petPair?.Key,
                Enabled = enabled,
                Height = height,
                StrengthMin = strengthMin,
                StrengthMax = strengthMax,
                StrengthStep = strengthStep
            };

            return TWNetClient.Instance.SendAsync(networkPacket, TWNetMessageType.HeightControl);
        }

        public static void SendButtplugUpdate(LeadPair? pair = null)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return;

            if (_buttplugUpdate != null && !_buttplugUpdate.IsCompleted)
                return;

            _buttplugUpdate = TwTask.Run(async () =>
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

                await TWNetClient.Instance.SendAsync(packet, TWNetMessageType.ButtplugUpdate);
            });
        }

        public static void SendMasterRemoteSettingsAsync(LeadPair? pair = null)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return;

            if (_remoteSettingsTask != null && !_remoteSettingsTask.IsCompleted)
                return;

            _remoteSettingsTask = TwTask.Run(async () =>
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
                    packet.Parameters = new List<MasterRemoteParameter>();
                    packet.AppliedFeatures |= pair.ForcedMute ? NetworkedFeature.AllowForceMute : NetworkedFeature.None;
                    packet.AppliedFeatures |= pair.DisableFlight ? NetworkedFeature.DisableFlight : NetworkedFeature.None;
                    packet.AppliedFeatures |= pair.DisableSeats ? NetworkedFeature.DisableSeats : NetworkedFeature.None;
                    packet.AppliedFeatures |= pair.Blindfold ? NetworkedFeature.AllowBlindfolding : NetworkedFeature.None;
                    packet.AppliedFeatures |= pair.Deafen ? NetworkedFeature.AllowDeafening : NetworkedFeature.None;
                    packet.AppliedFeatures |= pair.MasterDeafenBypass ? NetworkedFeature.MasterBypassDeafen : NetworkedFeature.None;

                    if (pair.TargetAvatar != null)
                    {
                        packet.TargetAvatar = pair.TargetAvatar;
                        pair.TargetAvatar = null;
                    }
                    
                    if (pair.PropTarget != null)
                        packet.PropTarget = pair.LockToProp ? pair.PropTarget : null;

                    if (pair.LeashPinPosition != Vector3.zero)
                        packet.LeashPinPosition = pair.LockToWorld ? pair.LeashPinPosition.ToTWVector3() : TWVector3.Zero;

                    var paramsUpdated = pair.PetEnabledParameters.Where(x => x.IsUpdated).ToList();

                    if (paramsUpdated.Count > 0)
                    {
                        pair.PetEnabledParameters.ForEach(x => x.IsUpdated = false);
                        packet.Parameters = paramsUpdated;
                    }
                }
                else
                {
                    packet.AppliedFeatures |= LeadManager.Instance.ForcedMute ? NetworkedFeature.AllowForceMute : NetworkedFeature.None;
                    packet.AppliedFeatures |= LeadManager.Instance.DisableFlight ? NetworkedFeature.DisableFlight : NetworkedFeature.None;
                    packet.AppliedFeatures |= LeadManager.Instance.DisableSeats ? NetworkedFeature.DisableSeats : NetworkedFeature.None;
                    packet.AppliedFeatures |= LeadManager.Instance.Blindfold ? NetworkedFeature.AllowBlindfolding : NetworkedFeature.None;
                    packet.AppliedFeatures |= LeadManager.Instance.Deafen ? NetworkedFeature.AllowDeafening : NetworkedFeature.None;
                    packet.AppliedFeatures |= LeadManager.Instance.MasterDeafenBypass ? NetworkedFeature.MasterBypassDeafen : NetworkedFeature.None;

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
                        keyPair.Value.MasterDeafenBypass = LeadManager.Instance.MasterDeafenBypass;
                        keyPair.Value.GlobalValuesUpdate = true;
                    }
                }

                await TWNetClient.Instance.SendAsync(packet, TWNetMessageType.MasterRemoteControl3);
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

            TwTask.Run(TWNetClient.Instance.SendAsync(packet, TWNetMessageType.LeadAccept));
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
                TempUnlockLeash = LeadManager.Instance.TempUnlockLeash,
                Key = key
            };

            packet.AppliedFeatures |= LeadManager.Instance.ForcedMute ? NetworkedFeature.AllowForceMute : NetworkedFeature.None;
            packet.AppliedFeatures |= LeadManager.Instance.DisableFlight ? NetworkedFeature.DisableFlight : NetworkedFeature.None;
            packet.AppliedFeatures |= LeadManager.Instance.DisableSeats ? NetworkedFeature.DisableSeats : NetworkedFeature.None;
            packet.AppliedFeatures |= LeadManager.Instance.Blindfold ? NetworkedFeature.AllowBlindfolding : NetworkedFeature.None;
            packet.AppliedFeatures |= LeadManager.Instance.Deafen ? NetworkedFeature.AllowDeafening : NetworkedFeature.None;

            TwTask.Run(TWNetClient.Instance.SendAsync(packet, TWNetMessageType.LeadAccept));
        }

        public static void SendLeashConfigUpdate(LeadPair? pair = null)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return;

            if (_leashConfigUpdate != null && !_leashConfigUpdate.IsCompleted)
                return;

            _leashConfigUpdate = TwTask.Run(async () =>
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

                await TWNetClient.Instance.SendAsync(packet, TWNetMessageType.LeashConfigUpdate);
            });
        }

        public static void SendPetConfigUpdate(UpdateType updateType)
        {
            if (!TWNetClient.Instance.IsTWNetConnected())
                return;

            //Check if we actually have a master lol
            if (LeadManager.Instance.MasterPair == null) return;

            if (_petConfigUpdate is { IsCompleted: false })
                return;

            _petConfigUpdate = TwTask.Run(async () =>
            {
                var timeBetweenLast = DateTime.Now.Subtract(_lastPetConfigUpdate).Milliseconds;

                //Ensure MasterRemoteSettings waits before being sent
                if (timeBetweenLast <= 100)
                    await Task.Delay(100 - timeBetweenLast);

                _lastPetConfigUpdate = DateTime.Now;

                var configUpdate = new PetConfigUpdate();

                if (updateType.HasFlag(UpdateType.AllowedFeaturesUpdate))
                {
                    configUpdate.AllowedFeatures |= ConfigManager.Instance.IsActive(AccessType.AllowForceMute, LeadManager.Instance.MasterPair.MasterID) ? NetworkedFeature.AllowForceMute : NetworkedFeature.None;
                    configUpdate.AllowedFeatures |= ConfigManager.Instance.IsActive(AccessType.AllowToyControl, LeadManager.Instance.MasterPair.MasterID) ? NetworkedFeature.AllowToyControl : NetworkedFeature.None;
                    configUpdate.AllowedFeatures |= ConfigManager.Instance.IsActive(AccessType.AllowWorldPropPinning, LeadManager.Instance.MasterPair.MasterID) ? NetworkedFeature.AllowPinning : NetworkedFeature.None;
                    configUpdate.AllowedFeatures |= ConfigManager.Instance.IsActive(AccessType.AllowMovementControls, LeadManager.Instance.MasterPair.MasterID) ? NetworkedFeature.DisableFlight : NetworkedFeature.None;
                    configUpdate.AllowedFeatures |= ConfigManager.Instance.IsActive(AccessType.AllowBlindfolding, LeadManager.Instance.MasterPair.MasterID) ? NetworkedFeature.AllowBlindfolding : NetworkedFeature.None;
                    configUpdate.AllowedFeatures |= ConfigManager.Instance.IsActive(AccessType.AllowDeafening, LeadManager.Instance.MasterPair.MasterID) ? NetworkedFeature.AllowDeafening : NetworkedFeature.None;
                    configUpdate.AllowedFeatures |= ConfigManager.Instance.IsActive(AccessType.AllowBeep, LeadManager.Instance.MasterPair.MasterID) ? NetworkedFeature.AllowBeep : NetworkedFeature.None;
                    configUpdate.AllowedFeatures |= ConfigManager.Instance.IsActive(AccessType.AllowVibrate, LeadManager.Instance.MasterPair.MasterID) ? NetworkedFeature.AllowVibrate : NetworkedFeature.None;
                    configUpdate.AllowedFeatures |= ConfigManager.Instance.IsActive(AccessType.AllowShock, LeadManager.Instance.MasterPair.MasterID) ? NetworkedFeature.AllowShock : NetworkedFeature.None;
                    configUpdate.AllowedFeatures |= ConfigManager.Instance.IsActive(AccessType.AllowHeightControl, LeadManager.Instance.MasterPair.MasterID) ? NetworkedFeature.AllowHeight : NetworkedFeature.None;
                    configUpdate.AllowedFeatures |= ConfigManager.Instance.IsActive(AccessType.AllowAnyAvatarSwitch, LeadManager.Instance.MasterPair.MasterID) ? NetworkedFeature.AllowAnyAvatarSwitching : NetworkedFeature.None;
                }

                if (updateType.HasFlag(UpdateType.AvatarListUpdate))
                {
                    configUpdate.AllowedAvatars = Configuration.JSONConfig.SwitchingAllowedAvatars;
                }

                if (updateType.HasFlag(UpdateType.RemoteParamUpdate))
                {
                    configUpdate.Parameters = new();

                    foreach (var enabled in AvatarParameterManager.Instance.TWAvatarParameters.Where(x => x.RemoteEnabled))
                    {
                        configUpdate.Parameters.Add(new MasterRemoteParameter()
                        {
                            ParameterTarget = enabled.Name,
                            ParameterValue = enabled.CurrentValue,
                            ParameterType = (int)enabled.ParamType,
                            ParameterOptions = enabled.Options
                        });
                    }
                }

                configUpdate.UpdateType = updateType;

                await TWNetClient.Instance.SendAsync(configUpdate, TWNetMessageType.PetConfigUpdate);
            });
        }
    }
}