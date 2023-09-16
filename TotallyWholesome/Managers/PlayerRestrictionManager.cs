using ABI_RC.Core;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.MovementSystem;
using TotallyWholesome.Managers.AvatarParams;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using TotallyWholesome.TWUI;
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
        
        public int Priority() => 0;
        public string ManagerName() => nameof(PlayerRestrictionManager);
        
        //Achievement flags
        public bool IsDeafened, IsBlindfolded;

        private GameObject _twBlindnessObject;
        private Material _blindnessMaterial;
        private SliderFloat _blindnessRadiusSlider;
        private static readonly int Radius = Shader.PropertyToID("_Radius");
        private AudioMixer _gameMainMixer;
        private AudioMixerGroup _twMixerGroup;
        private SliderFloat _deafenAttenuationSlider;

        public void Setup()
        {
            Instance = this;
            
            //TODO: Rename that slider id lmao
            _blindnessRadiusSlider = new SliderFloat("blindlessSlider", Configuration.JSONConfig.BlindnessRadius);
            _blindnessRadiusSlider.OnValueUpdated += f =>
            {
                if (_blindnessMaterial != null)
                    _blindnessMaterial.SetFloat(Radius, f);
                
                Configuration.JSONConfig.BlindnessRadius = f;
                Configuration.SaveConfig();
            };

            _deafenAttenuationSlider = new SliderFloat("deafenSlider", Configuration.JSONConfig.DeafenAttenuation);
            _deafenAttenuationSlider.OnValueUpdated += f =>
            {
                if (TWAssets.TWMixer != null)
                    TWAssets.TWMixer.SetFloat("AttenuationFloat", f);

                Configuration.JSONConfig.DeafenAttenuation = f;
                Configuration.SaveConfig();
            };
            
            TWNetListener.MasterRemoteControlEvent += MasterRemoteControlEvent;
            TWNetListener.LeadAcceptEvent += LeadAcceptEvent;
            LeadManager.OnFollowerPairDestroyed += OnFollowerPairDestroyed;
            Patches.OnWorldLeave += OnWorldLeave;
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

        private void LeadAcceptEvent(LeadAccept obj)
        {
            if (!obj.FollowerID.Equals(MetaPort.Instance.ownerId)) return;

            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (ConfigManager.Instance.IsActive(AccessType.AllowMovementControls, obj.MasterID)) 
                    ChangeMovementOptions(obj.DisableFlight, obj.DisableSeats);
                if(ConfigManager.Instance.IsActive(AccessType.AllowBlindfolding, obj.MasterID))
                    ApplyBlindfold(obj.BlindPet);
                if(ConfigManager.Instance.IsActive(AccessType.AllowDeafening, obj.MasterID))
                    ApplyDeafen(obj.DeafenPet);        
            });
        }

        private void MasterRemoteControlEvent(MasterRemoteControl obj)
        {
            if (LeadManager.Instance.MasterPair == null) return;
            if (!LeadManager.Instance.MasterPair.Key.Equals(obj.Key)) return;

            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (ConfigManager.Instance.IsActive(AccessType.AllowMovementControls, LeadManager.Instance.MasterPair.MasterID)) 
                    ChangeMovementOptions(obj.DisableFlight, obj.DisableSeats);
                
                if (ConfigManager.Instance.IsActive(AccessType.AllowBlindfolding, LeadManager.Instance.MasterPair.MasterID))
                    ApplyBlindfold(obj.BlindPet);    
                
                if(ConfigManager.Instance.IsActive(AccessType.AllowDeafening, LeadManager.Instance.MasterPair.MasterID))
                    ApplyDeafen(obj.DeafenPet);
            });
        }

        public void ApplyDeafen(bool deafen, bool silentSwitch = false)
        {
            if (deafen && _gameMainMixer.outputAudioMixerGroup == null)
            {
                _gameMainMixer.outputAudioMixerGroup = _twMixerGroup;
                AvatarParameterManager.Instance.TrySetParameter("TWDeafened", 1f);

                IsDeafened = true;
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has deafened you!", 3f, TWAssets.Handcuffs);
            }
            
            if (!deafen && _gameMainMixer.outputAudioMixerGroup != null)
            {
                _gameMainMixer.outputAudioMixerGroup = null;
                AvatarParameterManager.Instance.TrySetParameter("TWDeafened", 0f);

                IsDeafened = false;
                
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
                MovementSystem.Instance.ChangeFlight(false);
                
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
                if(MovementSystem.Instance.lastSeat != null)
                    MovementSystem.Instance.lastSeat.ExitSeat();
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has disabled seat usage!", 3f, TWAssets.Handcuffs);
            }

            if (!disableSeats && Patches.AreSeatsLocked)
            {
                Patches.AreSeatsLocked = false;
                
                if(!silentSwitch)
                    NotificationSystem.EnqueueNotification("Totally Wholesome", "Your master has allowed seat usage!", 3f, TWAssets.Checkmark);
            }
        }

        public void LateSetup()
        {
            var cameraGO = PlayerSetup.Instance.GetActiveCamera();

            _twBlindnessObject = Object.Instantiate(TWAssets.TWBlindness, cameraGO.transform);
            _twBlindnessObject.transform.localPosition = Vector3.zero;
            _twBlindnessObject.SetActive(false);

            _blindnessMaterial = _twBlindnessObject.transform.Find("Vision Sphere").GetComponent<MeshRenderer>().material;
            _blindnessMaterial.SetFloat(Radius, Configuration.JSONConfig.BlindnessRadius);

            Con.Debug("Instantiated TWBlindness prefab!");

            _gameMainMixer = RootLogic.Instance.mainSfx.audioMixer;
            _twMixerGroup = TWAssets.TWMixer.FindMatchingGroups("Master")[0];
            TWAssets.TWMixer.SetFloat("AttenuationFloat", Configuration.JSONConfig.DeafenAttenuation);
        }
    }
}