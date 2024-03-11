using System;
using System.Collections.Generic;
using ABI_RC.Core.Savior;
using ABI.CCK.Scripts;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using BTKUILib.UIObjects.Objects;
using TotallyWholesome.Managers.AvatarParams;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Shockers;
using TotallyWholesome.Network;
using TotallyWholesome.Objects;
using TWNetCommon;
using TotallyWholesome.Utils;
using TWNetCommon.Data.ControlPackets.Shockers;
using TWNetCommon.Data.ControlPackets.Shockers.Models;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome.Managers.TWUI.Pages;

public class IndividualPetControl : ITWManager
{
    public static IndividualPetControl Instance;

    //We want this ready before some other managers, some depend on seeing IPC parts

    public int Priority => 5;

    public LeadPair SelectedLeadPair;
    public string SelectedPetName, SelectedPetID;
    public ToggleButton GagPet, HeightControl;
    public Dictionary<string, Button> PetButtons = new();

    private Category _petSelectCat;
    private Page _remoteParamControl;

    private string _lastAvatarListPetID;

    //Param Control Elements
    private string _lastParamControlUserID;
    private LeadPair _paramControlLeadPair;
    private List<SliderFloat> _generatedRemoteControlSliders = new();
    private Category _generatedToggles;
    private Category _generatedMultiselected;
    private Category _generatedSingleInput;
    private Category _generatedSliderFloats;
    private Page _avatarSwitchingPage;
    private Category _petAvatarListCat;
    private Button _openAvatarSwitch, _removeLeash, _beep, _vibrate, _shock, _switchToMyAvi;

    //Individual Pet Controls
    public SliderFloat StrengthIPC;
    public SliderFloat DurationIPC;
    public SliderFloat ShockHeightIPC;
    public SliderFloat ShockHeightStrengthMinIPC;
    public SliderFloat ShockHeightStrengthMaxIPC;
    public SliderFloat ShockHeightStrengthStepIPC;

    public void Setup()
    {
        Instance = this;

        QuickMenuAPI.OnWorldLeave += () =>
        {
            foreach (var button in PetButtons.Values)
                button.Delete();

            PetButtons.Clear();

            SelectedLeadPair = null;
            SelectedPetName = null;
            SelectedPetID = null;
        };

    }

    public void LateSetup()
    {
        var ipcPage = TWMenu.Pages["IPC"];
        _petSelectCat = ipcPage.AddCategory("Selected Pet: None", true, false);

        var controls = ipcPage.AddCategory("Controls", true, false);
        TWMenu.Categories.Add("IPCControls", controls);

        _remoteParamControl = new Page("TotallyWholesome", "Remote Param Control");
        _generatedMultiselected = _remoteParamControl.AddCategory("MultiSelection");
        _generatedToggles = _remoteParamControl.AddCategory("Toggles");
        _generatedSingleInput = _remoteParamControl.AddCategory("Input Singles");
        _generatedSliderFloats = _remoteParamControl.AddCategory("Sliders");

        QuickMenuAPI.AddRootPage(_remoteParamControl);

        var openRemoteParamControl = controls.AddButton("Remote Param Control", "ListX3",
            "Opens the remote parameter control for the selected pet");
        openRemoteParamControl.OnPress += () => { OpenParamControl(SelectedPetID); };

        var moreRestrictions = controls.AddButton("Restrictions", "Ability", "Access pet restrictions, movement, blindness, deafening, world pinning and more!");
        moreRestrictions.OnPress += () =>
        {
            if (SelectedLeadPair == null)
            {
                QuickMenuAPI.ShowAlertToast("You don't have a pet selected!");
                return;
            }

            PetRestrictionsPage.Instance.OpenRestrictionsPage(SelectedLeadPair);
        };

        _avatarSwitchingPage = Page.GetOrCreatePage("TotallyWholesome", "Avatar Switching");
        _openAvatarSwitch = controls.AddButton("Avatar Switching", "Body", "Opens the avatar list for this pet, you can change their selected avatar from there!");
        _openAvatarSwitch.OnPress += OpenAvatarSwitching;
        var avatarControls = _avatarSwitchingPage.AddCategory("Controls", false);
        _switchToMyAvi = avatarControls.AddButton("Switch to My Avatar", "Body", "Switch this pet into your current avatar (Must be shared or public!)");
        _switchToMyAvi.OnPress += SwitchToMyAvatar;
        _petAvatarListCat = _avatarSwitchingPage.AddCategory("Switchable Avatars", true, false);

        _removeLeash = controls.AddButton("Remove Leash", "TWClose", "Remove this pets leash");
        _removeLeash.OnPress += LeadManager.RemoveLeashIPC;

        LeadManager.Instance.TetherRangeIPC =
            controls.AddSlider("Leash Length", "Adjust the length of this pets leash", 2f, 0f, 10f);
        ButtplugManager.Instance.ToyStrengthIPC =
            controls.AddSlider("Toy Strength", "Control the strength of this pets toys", 0f, 0f, 1f);

        ButtplugManager.Instance.ToyStrengthIPC.Hidden = ConfigManager.Instance.IsActive(AccessType.HideToyIntegration);

        var shockCategory = ipcPage.AddCategory("Shock Controls");
        TWMenu.Categories.Add("IPCShock", shockCategory);

        shockCategory.Hidden = ConfigManager.Instance.IsActive(AccessType.HidePiShock);

        _beep = shockCategory.AddButton("Beep", "VolumeMax", "Sends a beep of the set duration to the selected pet");
        _beep.OnPress += () => ShockerAction(ControlType.Sound);

        _vibrate = shockCategory.AddButton("Vibrate", "Star",
            "Sends a vibration of the set duration and intensity to the selected pet");
        _vibrate.OnPress += () => ShockerAction(ControlType.Vibrate);

        _shock = shockCategory.AddButton("Shock", "Bolt",
            "Sends a shock of the set duration and duration to the selected pet");
        _shock.OnPress += () => ShockerAction(ControlType.Shock);

        HeightControl = shockCategory.AddToggle("Height Control", "Enable the height control system", false);
        HeightControl.OnValueUpdated += _ => HeightControlUpdate();

        StrengthIPC = shockCategory.AddSlider("Strength", "Sets the strength of the shocks/vibrations. Scales according to limits", 0f, 0f, 100f);
        StrengthIPC.OnValueUpdated += _ => ShockerUpdate();
        DurationIPC = shockCategory.AddSlider("Duration", "Sets the duration of shocks/beeps/vibrations. Scales according to limits", 0f, 0f, 15f);
        DurationIPC.OnValueUpdated += _ => ShockerUpdate();
        
        ShockHeightIPC =
            shockCategory.AddSlider("Shock Height", "Shocker Height Control Max Height allowed", 0f, 0f, 4f);
        ShockHeightIPC.OnValueUpdated += _ => HeightControlUpdate();
        ShockHeightStrengthMinIPC = shockCategory.AddSlider("Shock Height Min Strength",
            "Shocker Height Control Min Strength", 0f, 0f, 1f);
        ShockHeightStrengthMinIPC.OnValueUpdated += _ => HeightControlUpdate();
        ShockHeightStrengthMaxIPC = shockCategory.AddSlider("Shock Height Max Strength",
            "Shocker Height Control Max Strength", 0f, 0f, 1f);
        ShockHeightStrengthMaxIPC.OnValueUpdated += _ => HeightControlUpdate();
        ShockHeightStrengthStepIPC = shockCategory.AddSlider("Shock Height Step Increase",
            "Shocker Height Control Step Increase", 0f, 0f, 1f);
        ShockHeightStrengthStepIPC.OnValueUpdated += _ => HeightControlUpdate();
        
        UpdateButtonStates(NetworkedFeature.None);
    }

    public void AddPet(TWPlayerObject follower)
    {
        var petButton = _petSelectCat.AddButton(follower.Username, follower.PlayerIconURL, $"Selects {follower.Username} for individual pet control", ButtonStyle.FullSizeImage);
        petButton.OnPress += () =>
        {
            SelectPet(follower.Username, follower.Uuid);
        };
        PetButtons.Add(follower.Uuid, petButton);
    }

    public void RemovePet(string uuid)
    {
        if (!PetButtons.TryGetValue(uuid, out var button)) return;

        button.Delete();
        PetButtons.Remove(uuid);
    }

    public void UpdateButtonStates(NetworkedFeature enabledFeatures)
    {
        _vibrate.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowVibrate);
        _shock.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowShock);
        _beep.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowBeep);

        StrengthIPC.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowVibrate) && !enabledFeatures.HasFlag(NetworkedFeature.AllowShock) && !enabledFeatures.HasFlag(NetworkedFeature.AllowBeep);
        DurationIPC.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowVibrate) && !enabledFeatures.HasFlag(NetworkedFeature.AllowShock) && !enabledFeatures.HasFlag(NetworkedFeature.AllowBeep);

        _openAvatarSwitch.Disabled = (SelectedLeadPair?.SwitchableAvatars == null || SelectedLeadPair.SwitchableAvatars.Count == 0) && !enabledFeatures.HasFlag(NetworkedFeature.AllowAnyAvatarSwitching);

        _switchToMyAvi.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowAnyAvatarSwitching);
        ButtplugManager.Instance.ToyStrengthIPC.Disabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowToyControl);

        var heightDisabled = !enabledFeatures.HasFlag(NetworkedFeature.AllowHeight);

        HeightControl.Disabled = heightDisabled;
        ShockHeightIPC.Disabled = heightDisabled;
        ShockHeightStrengthMinIPC.Disabled = heightDisabled;
        ShockHeightStrengthMaxIPC.Disabled = heightDisabled;
        ShockHeightStrengthStepIPC.Disabled = heightDisabled;
    }

    public void OpenParamControl(string userID)
    {
        var user = TWUtils.GetPlayerFromPlayerlist(userID);

        if (user == null)
        {
            QuickMenuAPI.ShowAlertToast("You don't have a pet selected!");
            return;
        }

        var leadPair = LeadManager.Instance.GetLeadPairForPet(user);

        if (leadPair == null)
        {
            QuickMenuAPI.ShowAlertToast("This user is not your pet!");
            return;
        }

        if (_lastParamControlUserID != null && _lastParamControlUserID.Equals(userID) && !leadPair.UpdatedEnabledParams)
        {
            //Menu is already configured
            _remoteParamControl.OpenPage();
            return;
        }

        if (leadPair.PetEnabledParameters.Count == 0)
        {
            QuickMenuAPI.ShowNotice("No Remote Params!", "This pet does not have any remote parameters enabled!");
            return;
        }

        _lastParamControlUserID = userID;
        _paramControlLeadPair = leadPair;

        //We gotta rebuild the param control menu, lets wait till it's cleared!
        foreach (var slider in _generatedRemoteControlSliders)
            slider.Delete();

        _generatedSingleInput.ClearChildren();
        _generatedToggles.ClearChildren();
        _generatedMultiselected.ClearChildren();
        _generatedSliderFloats.ClearChildren();

        _generatedRemoteControlSliders.Clear();

        //Let's build the page
        foreach (var param in _paramControlLeadPair.PetEnabledParameters)
        {
            switch ((CVRAdvancedSettingsEntry.SettingsType)param.ParameterType)
            {
                case CVRAdvancedSettingsEntry.SettingsType.GameObjectToggle:
                    var toggle = _generatedToggles.AddToggle(param.ParameterTarget,
                        $"Control the {param.ParameterTarget} toggle on your pet", param.ParameterValue > 0.5f);
                    toggle.OnValueUpdated += b =>
                    {
                        param.ParameterValue = b ? 1f : 0f;
                        param.IsUpdated = true;
                        AvatarParameterManager.Instance.SendUpdatedParameters(_paramControlLeadPair);
                    };
                    break;
                case CVRAdvancedSettingsEntry.SettingsType.GameObjectDropdown:
                    var multiSelect = new MultiSelection(param.ParameterTarget, param.ParameterOptions,
                        (int)Math.Round(param.ParameterValue));
                    multiSelect.OnOptionUpdated += i =>
                    {
                        param.ParameterValue = i;
                        param.IsUpdated = true;
                        AvatarParameterManager.Instance.SendUpdatedParameters(_paramControlLeadPair);
                    };
                    var selectOpen = _generatedMultiselected.AddButton(param.ParameterTarget, "ListX3",
                        $"Opens the multi selection for {param.ParameterTarget}");
                    selectOpen.OnPress += () => { QuickMenuAPI.OpenMultiSelect(multiSelect); };
                    break;
                case CVRAdvancedSettingsEntry.SettingsType.Slider:
                    var slider = _generatedSliderFloats.AddSlider(param.ParameterTarget,
                        $"Control the {param.ParameterTarget} slider on your pet", param.ParameterValue, 0f, 1f);
                    slider.OnValueUpdated += f =>
                    {
                        param.ParameterValue = f;
                        param.IsUpdated = true;
                        AvatarParameterManager.Instance.SendUpdatedParameters(_paramControlLeadPair);
                    };
                    _generatedRemoteControlSliders.Add(slider);
                    break;
                case CVRAdvancedSettingsEntry.SettingsType.InputSingle:
                    var singInput = _generatedSingleInput.AddButton(param.ParameterTarget, "Settings",
                        $"Opens the {param.ParameterTarget} single input page");
                    singInput.OnPress += () =>
                    {
                        QuickMenuAPI.OpenNumberInput(param.ParameterTarget, param.ParameterValue, f =>
                        {
                            param.ParameterValue = f;
                            param.IsUpdated = true;
                            AvatarParameterManager.Instance.SendUpdatedParameters(_paramControlLeadPair);
                        });
                    };
                    break;
            }
        }

        _remoteParamControl.OpenPage();
    }

    public void UpdateAvatarSwitching(LeadPair leadPair)
    {
        //Rebuild avatar list
        _petAvatarListCat.ClearChildren();

        if (leadPair.SwitchableAvatars != null)
        {
            foreach (var avatar in leadPair.SwitchableAvatars)
            {
                TWUtils.GetAvatarFromAPI(avatar, response =>
                {
                    //Create button with icon
                    var button = _petAvatarListCat.AddButton(response.Name, response.ImageUrl, $"Switch the selected pet into \"{response.Name}\"!", ButtonStyle.FullSizeImage);
                    button.OnPress += () =>
                    {
                        QuickMenuAPI.ShowConfirm("Are you sure?",
                            $"Are you sure you want to switch this pet into \"{response.Name}\"? Please make sure this avatar is appropriate for the instance type you're in!",
                            () =>
                            {
                                leadPair.TargetAvatar = response.Id;
                                TWNetSendHelpers.SendMasterRemoteSettingsAsync(leadPair);
                            });
                    };
                });
            }
        }

        leadPair.UpdatedSwitchableAvatars = false;
        _lastAvatarListPetID = leadPair.PetID;
    }

    private void SwitchToMyAvatar()
    {
        QuickMenuAPI.ShowConfirm("Are you sure?",
            "Are you sure you want to switch this pet into your current avatar? Please make sure this avatar is appropriate for the instance type you're in!",
            () =>
            {
                SelectedLeadPair.TargetAvatar = MetaPort.Instance.currentAvatarGuid;
                TWNetSendHelpers.SendMasterRemoteSettingsAsync(SelectedLeadPair);
            });
    }

    private void OpenAvatarSwitching()
    {
        if (SelectedLeadPair == null)
        {
            QuickMenuAPI.ShowAlertToast("You don't have a pet selected!");
            return;
        }

        if (SelectedPetID != _lastAvatarListPetID || SelectedLeadPair.UpdatedSwitchableAvatars)
        {
            UpdateAvatarSwitching(SelectedLeadPair);
        }

        _avatarSwitchingPage.OpenPage();
    }

    private void SelectPet(string petName, string petID)
    {
        SelectedPetName = petName;
        SelectedPetID = petID;

        var petPlayer = TWUtils.GetPlayerFromPlayerlist(petID);

        if (petPlayer == null)
        {
            Con.Error("Attempted to select a pet that doesn't exist!");
            QuickMenuAPI.ShowAlertToast("That pet no longer exists, let's clean that up...");
            RemovePet(petID);
            SelectedLeadPair = null;
            SelectedPetID = "";
            SelectedPetName = "";
            return;
        }

        var leadPair = LeadManager.Instance.GetLeadPairForPet(petPlayer);

        if (leadPair == null)
        {
            Con.Error("Selected LeadPair doesn't exist!");
            QuickMenuAPI.ShowAlertToast("That pet no longer exists, let's clean that up...");
            RemovePet(petID);
            SelectedLeadPair = null;
            SelectedPetID = "";
            SelectedPetName = "";
            return;
        }

        _petSelectCat.CategoryName = $"Selected Pet: {petName}";

        SelectedLeadPair = leadPair;
        leadPair.GlobalValuesUpdate = false;
        UpdateButtonStates(leadPair.EnabledFeatures);
        UpdateIPCPage();
    }

    private void UpdateIPCPage()
    {
        GagPet.ToggleValue = SelectedLeadPair.ForcedMute;
        HeightControl.ToggleValue = SelectedLeadPair.Shocker.HeightControl.Enabled;
        LeadManager.Instance.TetherRangeIPC.SetSliderValue(SelectedLeadPair.LeadLength);
        ButtplugManager.Instance.ToyStrengthIPC.SetSliderValue(SelectedLeadPair.ToyStrength);

        //Shock
        DurationIPC.SetSliderValue(SelectedLeadPair.Shocker.Duration / 1000f);
        StrengthIPC.SetSliderValue(SelectedLeadPair.Shocker.Intensity);
        ShockHeightIPC.SetSliderValue(SelectedLeadPair.Shocker.HeightControl.Height);
        ShockHeightStrengthMaxIPC.SetSliderValue(SelectedLeadPair.Shocker.HeightControl.StrengthMax);
        ShockHeightStrengthMinIPC.SetSliderValue(SelectedLeadPair.Shocker.HeightControl.StrengthMin);
        ShockHeightStrengthStepIPC.SetSliderValue(SelectedLeadPair.Shocker.HeightControl.StrengthStep);
    }

    private void ShockerAction(ControlType type)
    {
        if (SelectedLeadPair == null)
            return;

        TwTask.Run(ShockerManager.Instance.SendControlNetworked(type, SelectedLeadPair.Shocker.Intensity,
            SelectedLeadPair.Shocker.Duration, SelectedLeadPair));

        QuickMenuAPI.ShowAlertToast(
            $"Sent {type.ToString()} to {SelectedLeadPair.Pet.Username} for {SelectedLeadPair.Shocker.Duration / 1000f} seconds at {SelectedLeadPair.Shocker.Intensity}%!");
    }

    private void ShockerUpdate()
    {
        if (SelectedLeadPair == null)
            return;
        
        SelectedLeadPair.Shocker.Intensity = Convert.ToByte(StrengthIPC.SliderValue);
        SelectedLeadPair.Shocker.Duration = Convert.ToUInt16(DurationIPC.SliderValue * 1000);
    }

    private static readonly Color ColorShown = new Color(0, 1, 0, 0.3f);

    private void HeightControlUpdate()
    {
        if (SelectedLeadPair == null)
            return;
        
        SelectedLeadPair.Shocker.HeightControl.Enabled = HeightControl.ToggleValue;
        SelectedLeadPair.Shocker.HeightControl.Height = ShockHeightIPC.SliderValue;
        SelectedLeadPair.Shocker.HeightControl.StrengthMin = ShockHeightStrengthMinIPC.SliderValue;
        SelectedLeadPair.Shocker.HeightControl.StrengthMax = ShockHeightStrengthMaxIPC.SliderValue;
        SelectedLeadPair.Shocker.HeightControl.StrengthStep = ShockHeightStrengthStepIPC.SliderValue;
        
        TwTask.Run(TWNetSendHelpers.SendHeightControl(
            SelectedLeadPair.Shocker.HeightControl.Enabled,
            SelectedLeadPair.Shocker.HeightControl.Height,
            SelectedLeadPair.Shocker.HeightControl.StrengthMin,
            SelectedLeadPair.Shocker.HeightControl.StrengthMax,
            SelectedLeadPair.Shocker.HeightControl.StrengthStep,
            SelectedLeadPair));
    }
}