using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ABI_RC.Core.Networking.API.Responses;
using ABI_RC.Core.Savior;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using BTKUILib.UIObjects.Objects;
using MelonLoader;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.PlayerRestrictions;
using TotallyWholesome.Managers.Shockers;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
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

    //Notification page
    private MultiSelection _alignmentSelect;
    private SliderFloat _notifXSlider;
    private SliderFloat _notifYSlider;

    private Page _userManagePage;
    private Dictionary<string, ToggleButton> _userPermsToggles = new();
    private Dictionary<string, Category> _switchableAvatarList = new();
    private Page _switchableAvatarPage;

    //Preview coroutine handling
    private int _blindfoldPreviewSeconds = 0;
    private int _deafenPreviewSeconds = 0;

    public void Setup()
    {
        Instance = this;
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

        //Notification Config Page
        var notifPage = general.AddPage("Notification Config", "Megaphone", "Configure the placement and opacity of the TW notification system", "TotallyWholesome");
        var notifCat = notifPage.AddCategory("NotifCat", false);
        TWMenu.Categories.Add("SettingsNotifCat", notifCat);

        _alignmentSelect = new MultiSelection("Notification Alignment", Enum.GetNames(typeof(NotificationAlignment)), (int)Configuration.JSONConfig.NotificationAlignment);
        _alignmentSelect.OnOptionUpdated += i =>
        {
            Configuration.JSONConfig.NotificationAlignment = (NotificationAlignment)i;
            Configuration.SaveConfig();
            NotificationSystem.UpdateNotificationAlignment();
        };

        var alignmentButton = notifCat.AddButton("Notification Alignment", "Resize", "Change the alignment of the notification");
        alignmentButton.OnPress += () => QuickMenuAPI.OpenMultiSelect(_alignmentSelect);

        var customPlacementToggle = notifCat.AddToggle("Custom Placement", "Enable custom placement of the notification popup", Configuration.JSONConfig.NotificationCustomPlacement);
        customPlacementToggle.OnValueUpdated += b =>
        {
            _notifXSlider.Disabled = !b;
            _notifYSlider.Disabled = !b;
            Configuration.JSONConfig.NotificationCustomPlacement = b;
            Configuration.SaveConfig();
            NotificationSystem.UpdateNotificationAlignment();
        };

        var testNotif = notifCat.AddButton("Test Notification", "Megaphone", "Sends a test notification");
        testNotif.OnPress += () =>
        {
            NotificationSystem.EnqueueNotification("Test Notification", "This is a test notification!", 10f, TWAssets.Megaphone);
        };

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

        var leashColourButton = twnet.AddButton("Leash Colour Config", "Settings", "Change the colour of your side of the leash");
        leashColourButton.OnPress += () =>
        {
            QuickMenuAPI.OpenColourPicker(Configuration.JSONConfig.LeashColour, SaveLeashColor);
        };

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
                toggle.OnValueUpdated += b => { ConfigManager.Instance.SetActive(accessType, b, toggle, QuickMenuAPI.SelectedPlayerID); };
                _userPermsToggles.Add(accessType.ToString(), toggle);
            }
        }

        var blindnessVisionPicker = petCat.AddButton("Blindness Vision Colour", "Ranking", "Set the colour of the vision area while blindfolded");
        blindnessVisionPicker.OnPress += () =>
        {
            QuickMenuAPI.OpenColourPicker(Configuration.JSONConfig.BlindnessVisionColour, (color, s) =>
            {
                Configuration.JSONConfig.BlindnessVisionColour = color;
                Configuration.SaveConfig();

                if (PlayerRestrictionManager.Instance.BlindnessMaterial != null)
                    PlayerRestrictionManager.Instance.BlindnessMaterial.SetColor(PlayerRestrictionManager.DarknessColour, color);

                if (LeadManager.Instance.MasterPair != null && LeadManager.Instance.MasterPair.Blindfold) return;

                var startCoroutine = _blindfoldPreviewSeconds == 0;

                //Start preview for 3 seconds
                _blindfoldPreviewSeconds = 3;

                if (startCoroutine)
                    MelonCoroutines.Start(BlindfoldPreview());
            }, true);
        };

        //Add the pet settings sliders after main toggle generation so position isn't shit
        BlindnessRadiusSlider = petCat.AddSlider("Blindness Radius", "Adjust the radius of visibility while blindfolded", Configuration.JSONConfig.BlindnessRadius, 0f, 10f, 2, 2f, true);
        DeafenAttenuationSlider = petCat.AddSlider("Deafen Attenuation", "Adjusts the level of volume attenuation while deafened", Configuration.JSONConfig.DeafenAttenuation, -80f, 20f, 2, -35f, true);

        BlindnessRadiusSlider.OnValueUpdated += f =>
        {
            if (PlayerRestrictionManager.Instance.BlindnessMaterial != null)
                PlayerRestrictionManager.Instance.BlindnessMaterial.SetFloat(PlayerRestrictionManager.Radius, f);

            Configuration.JSONConfig.BlindnessRadius = f;
            Configuration.SaveConfig();

            if (LeadManager.Instance.MasterPair != null && LeadManager.Instance.MasterPair.Blindfold) return;

            var startCoroutine = _blindfoldPreviewSeconds == 0;

            //Start preview for 3 seconds
            _blindfoldPreviewSeconds = 3;

            if (startCoroutine)
                MelonCoroutines.Start(BlindfoldPreview());
        };

        DeafenAttenuationSlider.OnValueUpdated += f =>
        {
            if (TWAssets.TWMixer != null)
                TWAssets.TWMixer.SetFloat("AttenuationFloat", f);

            Configuration.JSONConfig.DeafenAttenuation = f;
            Configuration.SaveConfig();

            if (LeadManager.Instance.MasterPair != null && LeadManager.Instance.MasterPair.Deafen) return;

            var startCoroutine = _deafenPreviewSeconds == 0;

            //Start preview for 3 seconds
            _deafenPreviewSeconds = 3;

            if (startCoroutine)
                MelonCoroutines.Start(DeafenPreview());
        };

        //Notification slider creation, gotta be after ConfigManager element gen
        var alphaSlider = notifCat.AddSlider("Notification Opacity", "Adjust the transparency of the notification popup", Configuration.JSONConfig.NotificationAlpha, 0f, 1f, 2, .7f, true);
        alphaSlider.OnValueUpdated += f =>
        {
            Configuration.JSONConfig.NotificationAlpha = f;
            Configuration.SaveConfig();
        };

        _notifXSlider = notifCat.AddSlider("Notification X Position", "Adjust the X position of the notification popup on the HUD", Configuration.JSONConfig.NotificationX, -1200, 1200, 2, 0, true);
        _notifXSlider.Disabled = !Configuration.JSONConfig.NotificationCustomPlacement;
        _notifXSlider.OnValueUpdated += f =>
        {
            Configuration.JSONConfig.NotificationX = f;
            Configuration.SaveConfig();
            NotificationSystem.UpdateNotificationAlignment();
        };

        _notifYSlider = notifCat.AddSlider("Notification Y Position", "Adjust the Y position of the notification popup on the HUD", Configuration.JSONConfig.NotificationY, -600, 600, 2, 0, true);
        _notifYSlider.Disabled = !Configuration.JSONConfig.NotificationCustomPlacement;
        _notifYSlider.OnValueUpdated += f =>
        {
            Configuration.JSONConfig.NotificationY = f;
            Configuration.SaveConfig();
            NotificationSystem.UpdateNotificationAlignment();
        };

        //Let's add all the switchable avatars
        Configuration.JSONConfig.SwitchingAllowedAvatars.ForEach(x => TWUtils.GetAvatarFromAPI(x, CreateAvatarListEntry));
    }

    private void SaveLeashColor(Color unityColour, string htmlColour)
    {
        Configuration.JSONConfig.LeashColour = htmlColour;
        Configuration.SaveConfig();

        TWNetSendHelpers.SendLeashConfigUpdate();
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

    private IEnumerator BlindfoldPreview()
    {
        PlayerRestrictionManager.Instance.ApplyBlindfold(true, true);

        while (_blindfoldPreviewSeconds != 0)
        {
            _blindfoldPreviewSeconds--;
            yield return new WaitForSeconds(1f);
        }

        if(LeadManager.Instance.MasterPair == null || !LeadManager.Instance.MasterPair.Blindfold)
            PlayerRestrictionManager.Instance.ApplyBlindfold(false, true);
    }

    private IEnumerator DeafenPreview()
    {
        PlayerRestrictionManager.Instance.ApplyDeafen(true, true);

        while (_deafenPreviewSeconds != 0)
        {
            _deafenPreviewSeconds--;
            yield return new WaitForSeconds(1f);
        }

        if(LeadManager.Instance.MasterPair == null || !LeadManager.Instance.MasterPair.Deafen)
            PlayerRestrictionManager.Instance.ApplyDeafen(false, true);
    }
}