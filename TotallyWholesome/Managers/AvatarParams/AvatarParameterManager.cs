using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI.CCK.Scripts;
using cohtml;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Network;
using TotallyWholesome.Objects.ConfigObjects;
using TotallyWholesome.TWUI;
using TWNetCommon;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using TWNetCommon.Data.NestedObjects;
using WholesomeLoader;
using Yggdrasil.Logging;

namespace TotallyWholesome.Managers.AvatarParams
{
    public class AvatarParameterManager : ITWManager
    {
        public static AvatarParameterManager Instance;

        public List<AvatarParameter> TWAvatarParameters = new();

        private int _maxParams = 12;
        private bool _shouldUpdateParameters = false;
        private AvatarParameter _aaProfilesEntry;

        public int Priority() => -1;
        public string ManagerName() => nameof(AvatarParameterManager);

        public void Setup()
        {
            Instance = this;
            
            Patches.Patches.OnLocalAvatarReady += OnLocalAvatarReady;
            TWNetListener.MasterRemoteControlEvent += MasterRemoteControlEvent;
            UserInterface.Instance.OnBackAction += ShouldUpdate;
            UserInterface.Instance.OnOpenedPage += ShouldUpdate;
            LeadManager.OnFollowerPairCreated += UpdateEnabledParametersOnAccept;
        }
        
        public void LateSetup(){}

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

        public void SetParameterRemoteState(string name, bool state)
        {
            var param = TWAvatarParameters.FirstOrDefault(x => x.Name.Equals(name));
            var count = TWAvatarParameters.Count(x => x.RemoteEnabled);

            if (state)
                count++;
            else
                count--;

            if (param == null) return;

            if (count > 12)
            {
                UIUtils.ShowNotice("Max Parameters!", $"You have too many enabled remote parameters! You can have a max of {_maxParams}!");
                UIUtils.SetToggleState(name, false, "FloatParams", "AvatarRemote");
                return;
            }

            param.RemoteEnabled = state;
            _shouldUpdateParameters = true;

            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twAvatarRemoteUpdateHeader", count);
        }
        
        public void SendUpdatedParameters(LeadPair pair)
        {
            if (pair == null || pair.PetEnabledParameters.All(x => !x.IsUpdated))
                return;

            TWNetSendHelpers.SendMasterRemoteSettingsAsync(pair);
        }
        
        public void UpdateEnabledParameters(bool force = false)
        {
            if (!_shouldUpdateParameters && !force) return;
            
            AvatarConfig config = new AvatarConfig();
            config.AvatarID = MetaPort.Instance.currentAvatarGuid;
            config.EnabledRemoteParams = new List<string>();

            MasterRemoteControl remoteControl = new MasterRemoteControl();
            if(LeadManager.Instance.FollowerPair != null)
                remoteControl.Key = LeadManager.Instance.FollowerPair.Key;
            remoteControl.ParameterConfigureUpdate = true;
            remoteControl.Parameters = new List<MasterRemoteParameter>();

            foreach (var enabled in TWAvatarParameters.Where(x => x.RemoteEnabled))
            {
                config.EnabledRemoteParams.Add(enabled.Name);
                remoteControl.Parameters.Add(new MasterRemoteParameter()
                {
                    ParameterTarget = enabled.Name, 
                    ParameterValue = enabled.CurrentValue, 
                    ParameterType = (int)enabled.ParamType,
                    ParameterOptions = enabled.Options
                });
            }
                
            if(_shouldUpdateParameters)
                Configuration.SaveAvatarConfig(config.AvatarID, config);

            _shouldUpdateParameters = false;

            TWNetClient.Instance.Send(remoteControl, TWNetMessageTypes.MasterRemoteControl2);
        }

        private void UpdateEnabledParametersOnAccept(LeadPair pair)
        {
            UpdateEnabledParameters(true);
        }

        private void ShouldUpdate(string targetPage, string lastPage)
        {
            if (lastPage!=null && !lastPage.Equals("AvatarRemoteConfig")) return;

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
                if (packet.ParameterConfigureUpdate)
                {
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
                    return;
                }
                
                if (LeadManager.Instance.FollowerPair == null || !LeadManager.Instance.FollowerPair.Key.Equals(packet.Key))
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
            
            UserInterface.Instance.UpdateAvatarRemoteConfig();
            ShouldUpdate(null, null);
        }
    }
}