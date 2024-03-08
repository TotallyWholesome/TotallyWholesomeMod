using System;
using System.Collections.Generic;
using System.Linq;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Savior;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using BTKUILib.UIObjects.Objects;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Shockers;
using TotallyWholesome.Network;
using TotallyWholesome.Utils;
using TWNetCommon.Data.ControlPackets;
using TWNetCommon.Data.ControlPackets.Shockers.Models;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome.Managers.TWUI.Pages;

public class TWSettingsUI : ITWManager
{
    public static TWSettingsUI Instance;

    public int Priority => 8;

    private static bool _boneAttachPet;

    private static MultiSelection _branchSelection;
    private static MultiSelection _boneAttachPoint;

    public SliderFloat DeafenAttenuationSlider;
    public SliderFloat BlindnessRadiusSlider;

    //Leash colour config
    private SliderFloat _rSliderFloat;
    private SliderFloat _gSliderFloat;
    private SliderFloat _bSliderFloat;
    private CustomEngineOnFunction _colourPreviewUpdate;

    private Page _userManagePage;
    private Dictionary<string, ToggleButton> _userPermsToggles = new();
    private Dictionary<string, Category> _switchableAvatarList = new();
    private Page _switchableAvatarPage;


    public void Setup()
    {
        Instance = this;

        QuickMenuAPI.OnMenuGenerated += _ =>
        {
            OnColorChanged();
        };
    }

    public void LateSetup()
    {
        var settingsPage = TWMenu.Pages["Settings"];
        var general = settingsPage.AddCategory("General");
        TWMenu.Categories.Add("SettingsGeneral", general);

        if (WholesomeLoader.WholesomeLoader.AvailableVersions != null)
        {
            var branches = WholesomeLoader.WholesomeLoader.AvailableVersions.Select(x => x.Branch).ToArray();
            var prettyNames = WholesomeLoader.WholesomeLoader.AvailableVersions.Select(x => x.BranchPrettyName).ToArray();
            var index = Array.IndexOf(branches, Configuration.JSONConfig.SelectedBranch);
            if (index == -1)
                Array.IndexOf(branches, "live");
            _branchSelection = new MultiSelection("Branch Selection", prettyNames, index);

            _branchSelection.OnOptionUpdated += i =>
            {
                Configuration.JSONConfig.SelectedBranch = branches[i];
                Configuration.SaveConfig();

                QuickMenuAPI.ShowNotice("Notice", "Selecting a new branch will require you to restart ChilloutVR before it takes effect!");
            };
        }

        var branchSelect = general.AddButton("TW Branches", "ListX3", "Switch the active branch of TW that you're using");
        branchSelect.OnPress += () =>
        {
            if (_branchSelection == null)
            {
                QuickMenuAPI.ShowNotice("No Branches",
                    "You do not have an additional branches, if you should please check that your Rank Key is set! If you still have issues check the appropriate channels in the discord!");
                return;
            }

            QuickMenuAPI.OpenMultiSelect(_branchSelection);
        };

        var restartButtplug = general.AddButton("Restart Buttplug", "TurnOff", "Restarts buttplug, this may restart Intiface CLI if it was started by TW");
        restartButtplug.OnPress += ButtplugManager.RestartButtplug;

        var testToys = general.AddButton("Test Toys", "Star", "Sends a vibration to all connected toys (shockers/buttplug devices)");
        testToys.OnPress += () =>
        {
            ButtplugManager.Instance.BeepBoop();
            TwTask.Run(ShockerManager.Instance.UiControl(ControlType.Vibrate, 50, 2000));
        };

        var reloadConfig = general.AddButton("Reload Config", "Reload", "Reloads the Config Manager, this will reload configs from their files");
        reloadConfig.OnPress += ConfigManager.ReloadConfig;

        var menuPage = general.AddPage("Menu Settings", "Settings", "Control the visibility and position of some elements", "TotallyWholesome");
        TWMenu.Pages.Add("SettingsMenuAdjPage", menuPage);

        var menuPageCat = menuPage.AddCategory("Menu Settings");
        TWMenu.Categories.Add("MenuAdjMainCat", menuPageCat);

        var logoPosYSlider = menuPageCat.AddSlider("Logo Position Y", "Allows you to change the TW logo position Y, you can set it from -300 to 1460", Configuration.JSONConfig.LogoPositionY, -300, 1460, 0, 0, true);
        var logoPosXSlider = menuPageCat.AddSlider("Logo Position X", "Allows you to change the TW logo position X, you can set it from -300 to 1460", Configuration.JSONConfig.LogoPositionX, -300, 1460, 0, 1460, true);
        logoPosYSlider.OnValueUpdated += f =>
        {
            Configuration.JSONConfig.LogoPositionY = (int)f;
            Configuration.SaveConfig();
            TWMenu.TWStatusPosition.TriggerEvent(Configuration.JSONConfig.LogoPositionX, Configuration.JSONConfig.LogoPositionY, TWMenu.TWMenuButton.ElementID);
        };
        logoPosXSlider.OnValueUpdated += f =>
        {
            Configuration.JSONConfig.LogoPositionX = (int)f;
            Configuration.SaveConfig();
            TWMenu.TWStatusPosition.TriggerEvent(Configuration.JSONConfig.LogoPositionX, Configuration.JSONConfig.LogoPositionY, TWMenu.TWMenuButton.ElementID);
        };

        TWMenu.Categories.Add("MenuAdjHideCat", menuPage.AddCategory("Hide Elements"));

        _boneAttachPoint = new MultiSelection("Bone Attach Point", new[] { "Neck", "Spine", "Hips", "LeftFoot", "RightFoot", "LeftHand", "RightHand" }, 0);
        _boneAttachPoint.OnOptionUpdated += BoneAttachPointSelected;

        var masterCat = settingsPage.AddCategory("Master");
        TWMenu.Categories.Add("SettingsMaster", masterCat);
        var attachPointMaster = masterCat.AddButton("Master Lead Attach Point", "Body", "Select where you would like the lead to be attached to");
        attachPointMaster.OnPress += () =>
        {
            _boneAttachPet = false;
            _boneAttachPoint.Name = "Master Bone Attach Point";
            _boneAttachPoint.SelectedOption = Configuration.JSONConfig.MasterBoneTarget.GetLeadAttachIndexFromBodyBone();
            QuickMenuAPI.OpenMultiSelect(_boneAttachPoint);
        };

        var petCat = settingsPage.AddCategory("Pet");
        TWMenu.Categories.Add("SettingsPet", petCat);
        var attachPointPet = petCat.AddButton("Pet Lead Attach Point", "Body", "Select where you would like the lead to be attached to");
        attachPointPet.OnPress += () =>
        {
            _boneAttachPet = true;
            _boneAttachPoint.Name = "Pet Bone Attach Point";
            _boneAttachPoint.SelectedOption = Configuration.JSONConfig.PetBoneTarget.GetLeadAttachIndexFromBodyBone();
            QuickMenuAPI.OpenMultiSelect(_boneAttachPoint);
        };

        //Switchable avatar list
        _switchableAvatarPage = petCat.AddPage("Switchable Avatars", "ListX3", "This page let's you configure what avatars you can be changed into by a master!", "TotallyWholesome");
        var utilCategory = _switchableAvatarPage.AddCategory("Utils", false);
        var addAvatar = utilCategory.AddButton("Add Current Avatar", "Checkmark", "Add the current avatar you're in to the switchable avatar list!");
        addAvatar.OnPress += AddCurrentAvatar;
        var clearList = utilCategory.AddButton("Clear All Avatars", "TWTrash", "Remove all avatars from the switchable avatar list");
        clearList.OnPress += RemoveAllEnabledAvatars;

        var shock = settingsPage.AddCategory("Shock");
        TWMenu.Categories.Add("SettingsShock", shock);

        var twnet = settingsPage.AddCategory("TWNet");
        TWMenu.Categories.Add("SettingsTWNet", twnet);

        var disconnect = twnet.AddButton("Disconnect from TWNet", "Exit", "Disconnects you from TWNet, you will need to manually reconnect!");
        disconnect.OnPress += TWNetClient.Instance.DisconnectClient;

        var reconnect = twnet.AddButton("Reconnect to TWNet", "Reload", "Starts a reconnection to TWNet, only use this if you are having trouble reconnecting automatically!");
        reconnect.OnPress += () =>
        {
            if (TWNetClient.Instance.IsTWNetConnected())
            {
                QuickMenuAPI.ShowNotice("Notice", "You are already connected to TWNet, this function is only for reconnecting if a problem prevented you from reconnecting automatically!");
                return;
            }

            TWNetClient.Instance.DisconnectClient();
            TWNetClient.Instance.ConnectClient();
        };



        var leashStyle = twnet.AddButton("Leash Style", "ListX3", "Opens up the selection page for leash styles");
        leashStyle.OnPress += LeadManager.SelectLeashStyle;

        var leashColourPage = twnet.AddPage("Leash Colour Config", "Settings", "Change the colour of your leash", "TotallyWholesome");
        var top = leashColourPage.AddCategory("Top", false);
        var colourPreview = new CustomElement("""{"c":"col-3", "s":[{"c":"round-box", "a":{"id" : "twUI-LeashColorPreview"}}], "a":{"id":"[UUID]"}}""", ElementType.InCategoryElement, null, top);
        _colourPreviewUpdate = new CustomEngineOnFunction("twUpdateLeashPreview",
            """let element = document.getElementById("twUI-LeashColorPreview");element.style.backgroundColor = "#" + colour;""",
            new Parameter("colour", typeof(string), true, false)
        );
        colourPreview.AddEngineOnFunction(_colourPreviewUpdate);

        colourPreview.OnElementGenerated += OnColorChanged;

        top.AddCustomElement(colourPreview);

        var save = top.AddButton("Save", "Checkmark", "Save the current leash colour");
        save.OnPress += SaveColor;

        if (!ColorUtility.TryParseHtmlString(Configuration.JSONConfig.LeashColour, out var currentLeadColor))
        {
            currentLeadColor = Color.white;
        }

        _rSliderFloat = top.AddSlider("Red", "Adjust red value in leash colour", currentLeadColor.r, 0, 1);
        _gSliderFloat = top.AddSlider("Green", "Adjust green value in leash colour", currentLeadColor.g, 0, 1);
        _bSliderFloat = top.AddSlider("Blue", "Adjust blue value in leash colour", currentLeadColor.b, 0, 1);

        _rSliderFloat.OnValueUpdated += f => OnColorChanged();
        _gSliderFloat.OnValueUpdated += f => OnColorChanged();
        _bSliderFloat.OnValueUpdated += f => OnColorChanged();

        //User manage page
        _userManagePage = new Page("TotallyWholesome", "Manage User: ");
        QuickMenuAPI.AddRootPage(_userManagePage);

        TWMenu.Categories.Add("UserSettingsGeneral", _userManagePage.AddCategory("General"));
        TWMenu.Categories.Add("UserSettingsMaster", _userManagePage.AddCategory("Master"));
        TWMenu.Categories.Add("UserSettingsPet", _userManagePage.AddCategory("Pet"));
        TWMenu.Categories.Add("UserSettingsShock", _userManagePage.AddCategory("Shock"));
        TWMenu.Categories.Add("UserSettingsTWNet", _userManagePage.AddCategory("TWNet"));

        //Rebuild settings menu
        foreach (var item in Enum.GetValues(typeof(AccessType)))
        {
            var accessType = (AccessType)item;
            var attribute = accessType.GetAttributeOfType<AccessAttribute>();
            if (attribute.Global)
            {
                var category = TWMenu.Categories[attribute.Category];
                var toggle = category.AddToggle(attribute.Name, attribute.DescriptionGlobal, ConfigManager.Instance.IsActive(accessType));
                toggle.OnValueUpdated += b => { ConfigManager.Instance.SetActive(accessType, b, toggle); };
            }

            if (attribute.User)
            {
                var category = TWMenu.Categories[$"User{attribute.Category}"];
                var toggle = category.AddToggle(attribute.Name, attribute.DescriptionUser, ConfigManager.Instance.IsActive(accessType));
                toggle.OnValueUpdated += b => { ConfigManager.Instance.SetActive(accessType, b, toggle); };
                _userPermsToggles.Add(accessType.ToString(), toggle);
            }
        }

        //Add the pet settings sliders after main toggle generation so position isn't shit
        BlindnessRadiusSlider = petCat.AddSlider("Blindness Radius", "Adjust the radius of visibility while blindfolded", 2f, 0f, 10f);
        DeafenAttenuationSlider = petCat.AddSlider("Deafen Attenuation", "Adjusts the level of volume attenuation while deafened", -35f, -80f, 20f);

        BlindnessRadiusSlider.OnValueUpdated += f =>
        {
            if (PlayerRestrictionManager.Instance.BlindnessMaterial != null)
                PlayerRestrictionManager.Instance.BlindnessMaterial.SetFloat(PlayerRestrictionManager.Radius, f);

            Configuration.JSONConfig.BlindnessRadius = f;
            Configuration.SaveConfig();
        };

        DeafenAttenuationSlider.OnValueUpdated += f =>
        {
            if (TWAssets.TWMixer != null)
                TWAssets.TWMixer.SetFloat("AttenuationFloat", f);

            Configuration.JSONConfig.DeafenAttenuation = f;
            Configuration.SaveConfig();
        };

        //Let's add all the switchable avatars
        Configuration.JSONConfig.SwitchingAllowedAvatars.ForEach(x => TWUtils.GetAvatarFromAPI(x, CreateAvatarListEntry));
    }

    private void RemoveAllEnabledAvatars()
    {
        QuickMenuAPI.ShowConfirm("Remove all?", "Are you sure you'd like to clear all enabled switchable avatars?", () =>
        {
            Configuration.JSONConfig.SwitchingAllowedAvatars.Clear();
            Configuration.SaveConfig();
            foreach(var cat in _switchableAvatarList.Values)
                cat.Delete();
            _switchableAvatarList.Clear();
            QuickMenuAPI.ShowAlertToast("Removed all enabled avatars!");
            TWNetSendHelpers.SendPetConfigUpdate(UpdateType.AvatarListUpdate);
        });
    }

    private void AddCurrentAvatar()
    {
        if (Configuration.JSONConfig.SwitchingAllowedAvatars.Contains(MetaPort.Instance.currentAvatarGuid))
        {
            QuickMenuAPI.ShowAlertToast("This avatar is already in your switchable avatar list!");
            return;
        }

        if (Configuration.JSONConfig.SwitchingAllowedAvatars.Count >= 15)
        {
            QuickMenuAPI.ShowAlertToast("You have too many enabled avatars! You can only have a max of 15!");
            return;
        }

        Configuration.JSONConfig.SwitchingAllowedAvatars.Add(MetaPort.Instance.currentAvatarGuid);
        Configuration.SaveConfig();

        TWUtils.GetAvatarFromAPI(MetaPort.Instance.currentAvatarGuid, CreateAvatarListEntry);

        QuickMenuAPI.ShowAlertToast("Added current avatar to switchable avatar list!");
    }

    private void CreateAvatarListEntry(AvatarDetailsResponse avatarDetails)
    {
        var cat = _switchableAvatarPage.AddCategory(avatarDetails.Name);
        cat.AddButton("", avatarDetails.ImageUrl, avatarDetails.Description, ButtonStyle.FullSizeImage);
        var delete = cat.AddButton("Remove Avatar", "TWTrash", "Remove this avatar from the switchable avatar list");
        delete.OnPress += () =>
        {
            RemoveAvatarEntry(avatarDetails.Id);
        };
        _switchableAvatarList.Add(avatarDetails.Id, cat);
        TWNetSendHelpers.SendPetConfigUpdate(UpdateType.AvatarListUpdate);
    }

    private void RemoveAvatarEntry(string avatarID)
    {
        Configuration.JSONConfig.SwitchingAllowedAvatars.Remove(avatarID);
        _switchableAvatarList[avatarID].Delete();
        _switchableAvatarList.Remove(avatarID);
        TWNetSendHelpers.SendPetConfigUpdate(UpdateType.AvatarListUpdate);
    }

    public void OpenManageUser(string userID)
    {
        foreach (var item in Enum.GetValues(typeof(AccessType)))
        {
            var accessType = (AccessType)item;
            var attribute = accessType.GetAttributeOfType<AccessAttribute>();

            if (attribute.User)
            {
                _userPermsToggles[accessType.ToString()].ToggleValue = ConfigManager.Instance.IsActiveUserOnly(accessType, userID);
            }
        }

        var player = TWUtils.GetPlayerFromPlayerlist(userID);
        _userManagePage.PageDisplayName = $"Manage User: {player}";

        Con.Debug("Updated UserManagePage toggle states, opening menu.");
        _userManagePage.OpenPage();
    }

    private void OnColorChanged()
    {
        var color = new Color(_rSliderFloat.SliderValue, _gSliderFloat.SliderValue, _bSliderFloat.SliderValue);
        var colorhtml = ColorUtility.ToHtmlStringRGB(color);
        _colourPreviewUpdate.TriggerEvent(colorhtml);
    }

    private void SaveColor()
    {
        var newColor = new Color(_rSliderFloat.SliderValue, _gSliderFloat.SliderValue, _bSliderFloat.SliderValue);
        Configuration.JSONConfig.LeashColour = "#" + ColorUtility.ToHtmlStringRGB(newColor);
        Configuration.SaveConfig();

        TWNetSendHelpers.SendLeashConfigUpdate();
        QuickMenuAPI.GoBack();
    }

    private void BoneAttachPointSelected(int index)
    {
        var bone = TWUtils.GetBodyBoneFromLeadAttachIndex(index);

        if (bone == null)
        {
            Con.Error("Attach Point multiselection gave us an invalid index?!");
            return;
        }

        if (_boneAttachPet)
            Configuration.JSONConfig.PetBoneTarget = bone.Value;
        else
            Configuration.JSONConfig.MasterBoneTarget = bone.Value;

        Configuration.SaveConfig();
    }
}