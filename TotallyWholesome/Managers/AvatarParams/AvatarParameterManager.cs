using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI.CCK.Scripts;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using MelonLoader;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.TWUI;
using TotallyWholesome.Network;
using TotallyWholesome.Objects.ConfigObjects;
using TWNetCommon.Data.ControlPackets;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome.Managers.AvatarParams
{
    public class AvatarParameterManager : ITWManager
    {
        public static AvatarParameterManager Instance;

        public List<AvatarParameter> TWAvatarParameters = new();
        public int EnabledParams;
        public bool ChangedPetParam;

        private int _maxParams = 69;
        private bool _shouldUpdateParameters = false;
        private AvatarParameter _aaProfilesEntry;
        private Page _avatarRemoteConfPage;
        private Category _avatarRemoteParamToggles;
        private List<ToggleButton> _avatarParamGeneratedToggles = new();

        public int Priority => -1;

        public void Setup()
        {
            Instance = this;
            
            Patches.OnLocalAvatarReady += OnLocalAvatarReady;
            TWNetListener.MasterRemoteControlEvent += MasterRemoteControlEvent;
            TWNetListener.PetConfigUpdateEvent += PetConfigUpdateEvent;
            QuickMenuAPI.OnBackAction += ShouldUpdate;
            QuickMenuAPI.OnOpenedPage += ShouldUpdate;
        }

        private void PetConfigUpdateEvent(PetConfigUpdate packet)
        {
            if (!packet.UpdateType.HasFlag(UpdateType.RemoteParamUpdate))
                return;

            if (!LeadManager.Instance.ActiveLeadPairs.ContainsKey(packet.Key))
            {
                Con.Debug("ParameterConfigureUpdate came too early, ActiveLeadPair not yet registered!");
                return;
            }

            var leadPair = LeadManager.Instance.ActiveLeadPairs[packet.Key];

            if (!leadPair.IsPlayerInvolved(TWUtils.GetOurPlayer()))
                return;

            Con.Debug($"Updated LeadPair enabled remote params with {packet.Parameters.Count} params");

            leadPair.PetEnabledParameters = packet.Parameters;
            leadPair.UpdatedEnabledParams = true;
        }

        public void LateSetup()
        {
            _avatarRemoteConfPage = TWMenu.Pages["AvatarRemoteConfig"];
            _avatarRemoteParamToggles = _avatarRemoteConfPage.AddCategory("Parameters", false);
            _avatarRemoteConfPage.PageDisplayName = $"Avatar Remote Config - 0/{_maxParams} Selected";
            UpdateAvatarRemoteConfig();
        }

        public void TrySetParameter(string name, float value)
        {
            if (!Main.Instance.IsOnMainThread(Thread.CurrentThread))
            {
                Main.Instance.MainThreadQueue.Enqueue(() =>
                {
                    TrySetParameterMain(name, value);
                });

                return;
            }
            
            TrySetParameterMain(name, value);
        }

        public void SetParameterRemoteState(AvatarParameter param, ToggleButton toggle, bool state)
        {
            var count = TWAvatarParameters.Count(x => x.RemoteEnabled);

            if (state)
                count++;
            else
                count--;

            if (param == null) return;

            if (count > _maxParams)
            {
                QuickMenuAPI.ShowNotice("Max Parameters!", $"You have too many enabled remote parameters! You can have a max of {_maxParams}!");
                toggle.ToggleValue = false;
                return;
            }

            param.RemoteEnabled = state;
            _shouldUpdateParameters = true;

            _avatarRemoteConfPage.PageDisplayName = $"Avatar Remote Config - {count}/{_maxParams} Selected";
        }
        
        public void SendUpdatedParameters(LeadPair pair)
        {
            if (pair == null || pair.PetEnabledParameters.All(x => !x.IsUpdated))
                return;

            ChangedPetParam = true;

            TWNetSendHelpers.SendMasterRemoteSettingsAsync(pair);
        }

        public void TrySetTemporaryParameter(string parameterName, float parameterValue, float resetValue, float duration, float waitForStart = 0f)
        {
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                MelonCoroutines.Start(WaitForParamReset(parameterName, parameterValue, resetValue, duration, waitForStart));
            });
        }
        
        public void UpdateEnabledParameters(bool force = false)
        {
            if (!_shouldUpdateParameters && !force) return;
            
            AvatarConfig config = new AvatarConfig();
            config.AvatarID = MetaPort.Instance.currentAvatarGuid;
            config.EnabledRemoteParams = new List<string>();

            config.EnabledRemoteParams.AddRange(TWAvatarParameters.Where(e => e.RemoteEnabled).Select(x => x.Name).ToArray());

            EnabledParams = config.EnabledRemoteParams.Count;

            if (_shouldUpdateParameters)
            {
                Configuration.SaveAvatarConfig(config.AvatarID, config);
            }

            _shouldUpdateParameters = false;

            TWNetSendHelpers.SendPetConfigUpdate(UpdateType.RemoteParamUpdate);
        }

        private IEnumerator WaitForParamReset(string parameterName, float targetValue, float resetValue, float duration, float waitForStart)
        {
            if (waitForStart > 0)
                yield return new WaitForSeconds(waitForStart);
            
            TrySetParameterMain(parameterName, targetValue);
            
            yield return new WaitForSeconds(duration);
            
            TrySetParameterMain(parameterName, resetValue);
        }

        private void ShouldUpdate(string targetPage, string lastPage)
        {
            if (lastPage!=null && !lastPage.Equals(_avatarRemoteConfPage.ElementID)) return;

            UpdateEnabledParameters();
        }

        private void TrySetParameterMain(string name, float value)
        {
            PlayerSetup.Instance.animatorManager.SetAnimatorParameter(name, value);
        }
        
        private void MasterRemoteControlEvent(MasterRemoteControl packet)
        {
            if (packet.MasterGlobalControl)
                return;
            
            Main.Instance.MainThreadQueue.Enqueue(() =>
            {
                if (LeadManager.Instance.MasterPair == null || !LeadManager.Instance.MasterPair.Key.Equals(packet.Key))
                    return;

                foreach (var update in packet.Parameters)
                {
                    if (update.ParameterTarget.Equals("Avatar-Profiles"))
                    {
                        var profiles = _aaProfilesEntry?.Options;

                        if(profiles == null) continue;
                        var value = (int)Math.Round(update.ParameterValue);
                        if(value >= profiles.Length) continue;

                        if(value != 0)
                            PlayerSetup.Instance.LoadCurrentAvatarSettingsProfile(profiles[value]);
                        else
                            PlayerSetup.Instance.LoadCurrentAvatarSettingsDefault();

                        continue;
                    }

                    ChangedPetParam = true;
                    
                    PlayerSetup.Instance.animatorManager.SetAnimatorParameter(update.ParameterTarget, update.ParameterValue);
                    CVR_MenuManager.Instance.SendAdvancedAvatarUpdate(update.ParameterTarget, update.ParameterValue, false);
                }
            });
        }

        private void OnLocalAvatarReady()
        {
            TWAvatarParameters.Clear();
            
            var avatarDescriptor = PlayerSetup.Instance.GetLocalAvatarDescriptor();

            if (avatarDescriptor.avatarSettings.settings.Count == 0)
                return;

            var parameterUIObjects = PlayerSetup.Instance.getCurrentAvatarSettings();
            var config = Configuration.LoadAvatarConfig(MetaPort.Instance.currentAvatarGuid);

            foreach (var param in avatarDescriptor.avatarSettings.settings)
            {
                switch (param.type)
                {
                    case CVRAdvancedSettingsEntry.SettingsType.GameObjectToggle:
                    case CVRAdvancedSettingsEntry.SettingsType.Slider:
                    case CVRAdvancedSettingsEntry.SettingsType.InputSingle:
                    case CVRAdvancedSettingsEntry.SettingsType.GameObjectDropdown:
                        var uiObject = parameterUIObjects.FirstOrDefault(x => x.parameterName.Equals(param.machineName));
                        var parameter = new AvatarParameter()
                        {
                            Name = param.machineName,
                            CurrentValue = uiObject.defaultValueX,
                            ParamType = param.type,
                            Options = uiObject.optionList,
                            GeneratedType = uiObject.parameterType
                        };

                        if (config != null)
                            parameter.RemoteEnabled = config.EnabledRemoteParams.Contains(param.machineName);
                        
                        TWAvatarParameters.Add(parameter);
                        break;
                }
            }

            _aaProfilesEntry = new AvatarParameter()
            {
                Name = "Avatar-Profiles",
                CurrentValue = 0f,
                ParamType = CVRAdvancedSettingsEntry.SettingsType.GameObjectDropdown,
                GeneratedType = "dropdown"
            };

            var options = new List<string> { "Default" };

            var profiles = PlayerSetup.Instance.getCurrentAvatarSettingsProfiles();

            if (profiles != null)
                options.AddRange(profiles);

            _aaProfilesEntry.Options = options.ToArray();

            _aaProfilesEntry.CurrentValue = 0f;
            
            if (config != null)
                _aaProfilesEntry.RemoteEnabled = config.EnabledRemoteParams.Contains(_aaProfilesEntry.Name);

            TWAvatarParameters.Add(_aaProfilesEntry);

            _shouldUpdateParameters = true;

            UpdateAvatarRemoteConfig();

            ShouldUpdate(null, null);
        }

        private void UpdateAvatarRemoteConfig()
        {
            foreach(var toggle in _avatarParamGeneratedToggles)
                toggle.Delete();

            _avatarParamGeneratedToggles.Clear();

            var count = 0;

            foreach (var param in TWAvatarParameters)
            {
                var paramToggle = _avatarRemoteParamToggles.AddToggle(param.Name, $"Enables {param.Name} to be controlled by your master", param.RemoteEnabled);

                if (param.RemoteEnabled)
                    count++;

                paramToggle.OnValueUpdated += b =>
                {
                    SetParameterRemoteState(param, paramToggle, b);
                };

                _avatarParamGeneratedToggles.Add(paramToggle);
            }

            _avatarRemoteConfPage.PageDisplayName = $"Avatar Remote Config - {count}/{_maxParams} Selected";
        }
    }
}