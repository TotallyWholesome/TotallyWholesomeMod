using System.Linq;
using ABI_RC.Core;
using ABI_RC.Core.EventSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.Movement;
using TotallyWholesome.Managers.AvatarParams;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using TWNetCommon;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using UnityEngine;
using UnityEngine.Audio;
using WholesomeLoader;

namespace TotallyWholesome.Managers
{
    public class PlayerRestrictionManager : ITWManager
    {
        public static PlayerRestrictionManager Instance;
        public static readonly int Radius = Shader.PropertyToID("_Radius");
        public static readonly int DarknessColour = Shader.PropertyToID("_DarknessColor");

        public int Priority => 0;

        //Achievement flags
        public bool IsDeafened, IsBlindfolded;
        public Material BlindnessMaterial;
        public int AvatarSwitched;
        public bool MasterBypassApplied;

        private GameObject _twBlindnessObject;
        private AudioMixer _gameMainMixer;
        private AudioMixerGroup _twMixerGroup;

        public void Setup()
        {
            Instance = this;
            
            TWNetListener.MasterRemoteControlEvent += MasterRemoteControlEvent;
            TWNetListener.LeadAcceptEvent += LeadAcceptEvent;
            LeadManager.OnFollowerPairDestroyed += OnFollowerPairDestroyed;
            Patches.OnWorldLeave += OnWorldLeave;
            Patches.UserJoin += OnUserJoin;
        }

        public void LateSetup()
        {
            var cameraGO = PlayerSetup.Instance.GetActiveCamera();

            _twBlindnessObject = Object.Instantiate(TWAssets.TWBlindness, cameraGO.transform);
            _twBlindnessObject.transform.localPosition = Vector3.zero;
            _twBlindnessObject.SetActive(false);

            BlindnessMaterial = _twBlindnessObject.transform.Find("Vision Sphere").GetComponent<MeshRenderer>().material;
            BlindnessMaterial.SetFloat(Radius, Configuration.JSONConfig.BlindnessRadius);
            BlindnessMaterial.SetColor(DarknessColour, Configuration.JSONConfig.BlindnessVisionColour);

            Con.Debug("Instantiated TWBlindness prefab!");

            _gameMainMixer = RootLogic.Instance.mainSfx.audioMixer;
            _twMixerGroup = TWAssets.TWMixer.FindMatchingGroups("Master")[0];
            TWAssets.TWMixer.SetFloat("AttenuationFloat", Configuration.JSONConfig.DeafenAttenuation);
        }

        private void OnFollowerPairDestroyed(LeadPair obj)
        {
            ChangeMovementOptions(false, false, true);
            ApplyBlindfold(false, true);
            ApplyDeafen(false, true);
        }

        private void OnWorldLeave()
        {
            ChangeMovementOptions(false, false, true);
            ApplyBlindfold(false, true);
            ApplyDeafen(false, true);
        }

        private void LeadAcceptEvent(LeadAccept packet)
        {
            if (!packet.FollowerID.Equals(MetaPort.Instance.ownerId)) return;

            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (ConfigManager.Instance.IsActive(AccessType.AllowMovementControls, packet.MasterID))
                    ChangeMovementOptions(packet.AppliedFeatures.HasFlag(NetworkedFeature.DisableFlight), packet.AppliedFeatures.HasFlag(NetworkedFeature.DisableSeats));

                if (ConfigManager.Instance.IsActive(AccessType.AllowBlindfolding, packet.MasterID))
                    ApplyBlindfold(packet.AppliedFeatures.HasFlag(NetworkedFeature.AllowBlindfolding));

                if(ConfigManager.Instance.IsActive(AccessType.AllowDeafening, packet.MasterID))
                    ApplyDeafen(packet.AppliedFeatures.HasFlag(NetworkedFeature.AllowDeafening));
            });
        }

        private void MasterRemoteControlEvent(MasterRemoteControl packet)
        {
            if (LeadManager.Instance.MasterPair == null) return;
            if (!LeadManager.Instance.MasterPair.Key.Equals(packet.Key)) return;

            if (packet.TargetAvatar != null && (ConfigManager.Instance.IsActive(AccessType.AllowAnyAvatarSwitch, LeadManager.Instance.MasterPair.MasterID) || Configuration.JSONConfig.SwitchingAllowedAvatars.Contains(packet.TargetAvatar)))
            {
                //Avatar switch requested, we should fetch the details for this avatar
                TWUtils.GetAvatarFromAPI(packet.TargetAvatar, response =>
                {
                    //Queue up and fire!
                    if (!response.SwitchPermitted)
                    {
                        Con.Warn("Your master requested you switch into an avatar you have no access to!");
                        return;
                    }

                    Main.Instance.MainThreadQueue.Enqueue(() =>
                    {
                        AvatarSwitched++;
                        NotificationSystem.EnqueueNotification("Avatar Switching!", $"Your master has changed your avatar to \"{response.Name}\"!", 5f, TWAssets.Handcuffs);
                        AssetManagement.Instance.LoadLocalAvatar(packet.TargetAvatar);
                    });
                });
            }

            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (ConfigManager.Instance.IsActive(AccessType.AllowMovementControls, LeadManager.Instance.MasterPair.MasterID)) 
                    ChangeMovementOptions(packet.AppliedFeatures.HasFlag(NetworkedFeature.DisableFlight), packet.AppliedFeatures.HasFlag(NetworkedFeature.DisableSeats));
                
                if (ConfigManager.Instance.IsActive(AccessType.AllowBlindfolding, LeadManager.Instance.MasterPair.MasterID))
                    ApplyBlindfold(packet.AppliedFeatures.HasFlag(NetworkedFeature.AllowBlindfolding));
                
                if(ConfigManager.Instance.IsActive(AccessType.AllowDeafening, LeadManager.Instance.MasterPair.MasterID))
                    ApplyDeafen(packet.AppliedFeatures.HasFlag(NetworkedFeature.AllowDeafening), false, packet.AppliedFeatures.HasFlag(NetworkedFeature.MasterBypassDeafen));
            });
        }

        private void OnUserJoin(CVRPlayerEntity obj)
        {
            if (!IsDeafened || (LeadManager.Instance.MasterId == obj.Uuid && MasterBypassApplied)) return;

            var audioSource = obj.PuppetMaster.GetPlayerCommsAudioSource();
            if (audioSource != null)
                audioSource.outputAudioMixerGroup = _twMixerGroup;
        }

        public void ApplyDeafen(bool deafen, bool silentSwitch = false, bool masterBypass = false)
        {
            if ((deafen && !IsDeafened) || (masterBypass != MasterBypassApplied && deafen))
            {
                _gameMainMixer.outputAudioMixerGroup = _twMixerGroup;
                AvatarParameterManager.Instance.TrySetParameter("TWDeafened", 1f);

                foreach (var player in CVRPlayerManager.Instance.NetworkPlayers)
                {
                    var vivoxSource = player.PuppetMaster.GetPlayerCommsAudioSource();
                    if(vivoxSource != null)
                        vivoxSource.outputAudioMixerGroup = masterBypass && player.Uuid == LeadManager.Instance.MasterId ? null : _twMixerGroup;
                }

                if(!silentSwitch && !IsDeafened)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has deafened you!", 3f, TWAssets.Handcuffs);

                IsDeafened = true;
                MasterBypassApplied = masterBypass;
            }
            
            if (!deafen && IsDeafened)
            {
                _gameMainMixer.outputAudioMixerGroup = null;
                AvatarParameterManager.Instance.TrySetParameter("TWDeafened", 0f);

                foreach (var player in CVRPlayerManager.Instance.NetworkPlayers)
                {
                    var vivoxSource = player.PuppetMaster.GetPlayerCommsAudioSource();
                    if(vivoxSource != null)
                        vivoxSource.outputAudioMixerGroup = null;
                }

                IsDeafened = false;
                MasterBypassApplied = false;
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master undeafened you!", 3f, TWAssets.Handcuffs);
            }
        }

        public void ApplyBlindfold(bool blindfold, bool silentSwitch = false)
        {
            if (blindfold && !_twBlindnessObject.activeSelf)
            {
                _twBlindnessObject.SetActive(true);
                AvatarParameterManager.Instance.TrySetParameter("TWBlindfold", 1f);

                IsBlindfolded = true;
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has blindfolded you!", 3f, TWAssets.Handcuffs);
            }
            
            if (!blindfold && _twBlindnessObject.activeSelf)
            {
                _twBlindnessObject.SetActive(false);
                AvatarParameterManager.Instance.TrySetParameter("TWBlindfold", 0f);

                IsBlindfolded = false;
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master removed your blindfold!", 3f, TWAssets.Handcuffs);
            }
        }

        public void ChangeMovementOptions(bool disableFlight, bool disableSeats, bool silentSwitch = false)
        {
            if (disableFlight && !Patches.IsFlightLocked)
            {
                Patches.IsFlightLocked = true;
                BetterBetterCharacterController.Instance.ChangeFlight(false, false);
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has disabled flight!", 3f, TWAssets.Handcuffs);
            }

            if (!disableFlight && Patches.IsFlightLocked)
            {
                Patches.IsFlightLocked = false;
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has allowed flight!", 3f, TWAssets.Checkmark);
            }

            if (disableSeats && !Patches.AreSeatsLocked)
            {
                Patches.AreSeatsLocked = true;
                BetterBetterCharacterController.Instance.SetSitting(false);

                if (!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has disabled seat usage!", 3f, TWAssets.Handcuffs);
            }

            if (!disableSeats && Patches.AreSeatsLocked)
            {
                Patches.AreSeatsLocked = false;
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has allowed seat usage!", 3f, TWAssets.Checkmark);
            }
        }
    }
}