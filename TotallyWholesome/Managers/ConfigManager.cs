﻿using System;
using System.Collections.Generic;
using System.IO;
using BTKUILib;
using BTKUILib.UIObjects.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Shockers;
using TotallyWholesome.Managers.Shockers.PiShock;
using TotallyWholesome.Managers.Status;
using TotallyWholesome.Managers.TWUI;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using TWNetCommon;
using TWNetCommon.Data.ControlPackets;
using WholesomeLoader;

namespace TotallyWholesome.Managers
{
    public class ConfigManager : ITWManager
    {
        public static ConfigManager Instance;

        private Dictionary<AccessType, bool> _globalEntries; // per perm
        private Dictionary<string, List<AccessType>> _userEntries; // per user
        private string _settingsPathGlobal = Path.Combine(Configuration.SettingsPath, "GlobalSetttings.json");
        private string _settingsPathUser = Path.Combine(Configuration.SettingsPath, "UserSettings.json");
        private static Dictionary<AccessType, NetworkedFeature> _accessTypeFeatureMap;

        public int Priority => 9;

        public void Setup()
        {
            Instance = this;
            if (File.Exists("UserData/TWUserWhitelist.json"))
            {
                File.Delete("UserData/TWUserWhitelist.json");
                Con.Msg("Deleted old User Permissions file");
            }
            File.Delete("UserData/TotallyWholesome/GlobalPermissions.json");
            File.Delete("UserData/TotallyWholesome/UserPermissions.json");

            try
            {
                _globalEntries = File.Exists(_settingsPathGlobal) ? JsonConvert.DeserializeObject<Dictionary<AccessType, bool>>(File.ReadAllText(_settingsPathGlobal)) : new Dictionary<AccessType, bool>();
            }
            catch (Exception e)
            {
                _globalEntries = new Dictionary<AccessType, bool>();
                Con.Msg("An error occured while loading global settings! File reset!");
                Con.Error(e.Message); }
            try
            {
                _userEntries = File.Exists(_settingsPathUser) ? JsonConvert.DeserializeObject<Dictionary<string, List<AccessType>>>(File.ReadAllText(_settingsPathUser), new JsonSerializerSettings() { Converters = new List<JsonConverter>() { new StringEnumConverter() } }) : new Dictionary<string, List<AccessType>>();
            }
            catch (Exception e)
            {
                _userEntries = new Dictionary<string, List<AccessType>>();
                Con.Msg("An error occured while loading user settings! File reset!");
                Con.Error(e.Message);
            }
            _globalEntries ??= new Dictionary<AccessType, bool>();
            _userEntries ??= new Dictionary<string, List<AccessType>>();

            _accessTypeFeatureMap = new Dictionary<AccessType, NetworkedFeature>();

            foreach (var item in (AccessType[])Enum.GetValues(typeof(AccessType)))
            {
                var attribute = item.GetAttributeOfType<AccessAttribute>();

                if(attribute.FeatureEnum == NetworkedFeature.None) continue;

                _accessTypeFeatureMap.Add(item, attribute.FeatureEnum);
            }

            SaveToFile();

            Con.Msg($"Loaded {_userEntries.Count} user Settings sets and {_globalEntries.Count} global Settings!");

            //TODO: Add lead accept enabled features update
        }

        public static NetworkedFeature GetNetworkedFeatureEnum(AccessType accessType)
        {
            return !_accessTypeFeatureMap.TryGetValue(accessType, out var value) ? NetworkedFeature.None : value;
        }

        public void LateSetup()
        {

        }

        public static void ReloadConfig()
        {
            Configuration.Initialize();
            Instance.Setup();
        }
        
        /// <summary>
        /// Auto fills userId for IsActive with the current MasterId if there is
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsActiveCurrent(AccessType type) => IsActive(type, LeadManager.Instance.MasterId);

        public bool IsActive(AccessType type, string userId = null)
        {
            if (userId != null)
            {
                if (_userEntries.TryGetValue(userId, out var userPermissions))
                {
                    if (userPermissions.Contains(type))
                    {
                        return true;
                    }
                }
            }
            if (_globalEntries.TryGetValue(type, out bool value))
            {
                return value;
            }

            return type.GetAttributeOfType<AccessAttribute>().Default;
        }

        public bool IsActiveUserOnly(AccessType type, string userId)
        {
            if (_userEntries.TryGetValue(userId, out var userPermissions))
            {
                return userPermissions.Contains(type);

            }

            return false;
        }

        public void SetActive(AccessType type, bool value, ToggleButton toggle, string userId = null, bool acceptPopup = false)
        {
            var attribute = type.GetAttributeOfType<AccessAttribute>();

            if (attribute != null && value != attribute.Default)
            {
                //If value is set to true and confirmprompt is set show dialog
                if (attribute.ConfirmPrompt && toggle != null && !acceptPopup)
                {
                    QuickMenuAPI.ShowConfirm(attribute.Name, userId == null ? attribute.DialogMessageGlobal : attribute.DialogMessageUser, () =>
                    {
                        SetActive(type, value, toggle, userId, true);
                    }, () =>
                    {
                        SetActive(type, attribute.Default, toggle, userId);
                        toggle.ToggleValue = attribute.Default;
                    }, "Confirm", "Cancel");

                    return;
                }

                if (attribute.DialogMessageGlobal != null && userId == null && !acceptPopup)
                {
                    QuickMenuAPI.ShowNotice("Notice", attribute.DialogMessageGlobal);
                }

                if (attribute.DialogMessageUser != null && userId != null && !acceptPopup)
                {
                    QuickMenuAPI.ShowNotice("Notice", attribute.DialogMessageUser);
                }
            }

            if (userId != null)
            {
                if (!_userEntries.TryGetValue(userId, out var userPermissions))
                {
                    userPermissions = new List<AccessType>();
                    _userEntries[userId] = userPermissions;
                }
                if (value && !userPermissions.Contains(type))
                {
                    userPermissions.Add(type);
                }
                else if (!value && userPermissions.Contains(type))
                {
                    userPermissions.Remove(type);
                }

                if (LeadManager.Instance.MasterId == userId && attribute != null && attribute.FeatureEnum != NetworkedFeature.None)
                {
                    //Send our petconfigupdate
                    TWNetSendHelpers.SendPetConfigUpdate(UpdateType.AllowedFeaturesUpdate);
                }
            }
            else
            {
                _globalEntries[type] = value;

                if(LeadManager.Instance.MasterPair != null && attribute != null && attribute.FeatureEnum != NetworkedFeature.None)
                    TWNetSendHelpers.SendPetConfigUpdate(UpdateType.AllowedFeaturesUpdate);
            }

            OnChange(type, value, userId);

            SaveToFile();
        }

        private void OnChange(AccessType type, bool value, string userId = null)
        {
            switch (type)
            {
                case AccessType.EnableToyControl:
                case AccessType.AllowToyControl:
                    if (!value)
                        ButtplugManager.Instance.ResetToys();
                    break;
                case AccessType.FollowMasterWorldChange:
                    if (!value)
                        TWNetClient.Instance.AbortInstanceChange();
                    break;
                //TODO: MUFFLE
                /*case AccessType.EnableMuffledMode:
                    if (!value)
                        AudioManagement.SetMicrophoneActive(false);
                    break;*/
                case AccessType.UseOldHudMessage:
                    NotificationSystem.UseCVRNotificationSystem = value;
                    break;
                case AccessType.AllowHeightControl:
                    // PiShockManager.Instance.Reset();
                    break;
                case AccessType.HideToyIntegration:
                    ButtplugManager.Instance.ToyStrengthIPC.Hidden = value;
                    ButtplugManager.Instance.ToyStrength.Hidden = value;
                    break;
                case AccessType.HidePiShock:
                    TWMenu.Categories["MainShock"].Hidden = value;
                    TWMenu.Categories["IPCShock"].Hidden = value;
                    break;
                case AccessType.HideNameplateBadges:
                    StatusManager.Instance.SetTWBadgeHideStatus(value);
                    break;
                case AccessType.AllowBlindfolding:
                    PlayerRestrictionManager.Instance.ApplyBlindfold(false, true);
                    break;
                case AccessType.AllowMovementControls:
                    PlayerRestrictionManager.Instance.ChangeMovementOptions(false, false, true);
                    break;
                case AccessType.AllowDeafening:
                    PlayerRestrictionManager.Instance.ApplyDeafen(false, true);
                    break;
                case AccessType.AutoAcceptMasterRequest:
                case AccessType.AutoAcceptPetRequest:
                case AccessType.AutoAcceptFriendsOnly:
                    StatusManager.Instance.SendStatusUpdate();
                    break;
                default:
                    break;
            }
        }

        private void SaveToFile()
        {
            File.WriteAllText(_settingsPathGlobal, JsonConvert.SerializeObject(_globalEntries));
            File.WriteAllText(_settingsPathUser, JsonConvert.SerializeObject(_userEntries, new JsonSerializerSettings() { Converters = new List<JsonConverter>() { new StringEnumConverter() } }));
            Con.Debug("Saved Permissions Config");
        }
    }
    [Flags]
    public enum AccessType
    {
        [Access(Category = "SettingsGeneral", Name = "Hide the leash",
            DescriptionGlobal = "This will hide the leash for all your Pets/Masters",
            DescriptionUser = "This will hide the leash for this your Pets/Masters",
            Global = true, User = true,
            DialogMessageGlobal = "Changing No Visible Leash will not apply until you clear and create new leashes!",
            DialogMessageUser = "Changing No Visible Leash will not apply until you clear and create it again!")]
        NoVisibleLeash = 0x0001,

        [Access(Category = "SettingsGeneral", Name = "Private Leash",
            DescriptionGlobal = "Only you and your Pets/Masters will see the Leash",
            DescriptionUser = "Only you and this Pet/Master will see the Leash",
            Global = true, User = true,
            DialogMessageGlobal = "Changing Private Leash will not apply until you clear and create new leashes!",
            DialogMessageUser = "Changing Private Leash will not apply until you clear create it again!")]
        PrivateLeash = 0x0002,

        [Access(Category = "SettingsGeneral", Name = "Auto Accept Requests from Friends only",
            DescriptionGlobal = "Only people you have friended will be allowed to make your their Pet/Master",
            Global = true, User = false)]
        AutoAcceptFriendsOnly = 0x0003,

        [Access(Category = "SettingsGeneral", Name = "Block User",
            DescriptionUser = "Block this user from interacting with you",
            Global = false, User = true)]
        BlockUser = 0x0004,

        /*[Access(Category = "General", Name = "Use Tab Menu",
            DescriptionGlobal = "Toggles if Totally Wholesome uses a tab or a button in the launchpad",
            Global = true, User = false, Default = true)]
        UseTabMenu = 0x0005,*/

        [Access(Category = "SettingsNotifCat", Name = "Disable TW Notification System",
            DescriptionGlobal = "Disables the TW Notification system, if available it will use the NotificationAPI mod otherwise it'll use the CVR hud notifications",
            Global = true, User = false,
            DialogMessageGlobal = "Are you sure you want to disable the TW notification system? Disabling it will make TW use either the CVR hud notifications or the NotificationAPI mod if available.",
            ConfirmPrompt = true)]
        UseOldHudMessage = 0x0006,

        [Access(Category = "SettingsGeneral", Name = "Pet/Master Join Notifications",
            DescriptionGlobal = "Shows a popup notification when your master or pet joins your current instance",
            Global = true, User = false, Default = true)]
        MasterPetJoinNotification = 0x0007,

        [Access(Category = "SettingsGeneral", Name = "Use ActionMenu Controls",
            DescriptionGlobal = "Toggles the use of some pet controls in your Action Menu",
            Global = true, User = false, Default = true)]
        UseActionMenu = 0x0008,
        
        [Access(Category = "SettingsGeneral", Name = "Hide TW Nameplate Badges", DescriptionGlobal = "Hides the TW nameplate badge that is seen on other TW users", Global = true, Default = false)]
        HideNameplateBadges = 0x0009,

        [Access(Category = "SettingsPet", Name = "Auto Accept Pet Request",
            DescriptionGlobal = "This will allow ANYONE to make you their pet automatically",
            DescriptionUser = "This will allow this user to make you their pet automatically",
            Global = true, User = true,
            DialogMessageGlobal = "Are you sure you want to enable this? This will allow ANYONE to make you their pet!",
            DialogMessageUser = "Are you sure you want to enable this? This will allow this user to make you their pet without confirmation!",
            ConfirmPrompt = true)]
        AutoAcceptPetRequest = 0x1001,

        [Access(Category = "SettingsPet", Name = "Allow Force Mute",
            DescriptionGlobal = "This will allow all masters to mute you",
            DescriptionUser = "This will allow this master to mute you",
            Global = true, User = true,
            DialogMessageGlobal = "Allowing Force Mute will allow your master to mute you at any time! You can disable this setting to unlock it.",
            DialogMessageUser = "Allowing Force Mute will allow your master to mute you at any time! You can disable this setting to unlock it.", FeatureEnum = NetworkedFeature.AllowForceMute)]
        AllowForceMute = 0x1002,

        //TODO: MAKE THIS WORK AGAIN AAAAA
        /*[Access(Category = "SettingsPet", Name = "Enable Muffled Mode",
            DescriptionGlobal = "Instead of muted you will be muffled",
            Global = true, User = false)]
        EnableMuffledMode = 0x1003,*/

        [Access(Category = "SettingsPet", Name = "Enable Toy Control",
            DescriptionGlobal = "Enable Buttplug (Requires Restart)",
            Global = true, User = false, DialogMessageGlobal = "Enabling toy control will require you to restart before everything works!")]
        EnableToyControl = 0x1004,

        [Access(Category = "SettingsPet", Name = "Allow Toy Control",
            DescriptionGlobal = "This will allow all masters to control your Toys",
            DescriptionUser = "This will allow this master to control your Toys",
            Global = true, User = true,
            DialogMessageGlobal = "Allowing Toy Control will allow your master to control your connected toys! You can disable this setting to reset it.",
            DialogMessageUser = "Allowing Toy Control will allow your master to control your connected toys! You can disable this setting to reset it.", FeatureEnum = NetworkedFeature.AllowToyControl)]
        AllowToyControl = 0x1005,

        [Access(Category = "SettingsPet", Name = "Follow Master on World Change",
            DescriptionGlobal = "This will make you follow all masters to the new World",
            DescriptionUser = "This will make you follow this master to the new World",
            Global = true, User = true,
            DialogMessageGlobal = "Follow Master World Changes will automatically follow your master to their new instance if they have it enabled, you will have 10 seconds before leaving!",
            DialogMessageUser = "Follow Master World Changes will automatically follow your master to their new instance if they have it enabled, you will have 10 seconds before leaving!")]
        FollowMasterWorldChange = 0x1006,
        
        [Access(Category = "SettingsPet", Name = "Allow World/Prop Pinning", DescriptionGlobal = "Allows all masters to pin your leash to a part of the world or to a prop", DescriptionUser = "Allows this master to pin your leash to part of the world or to a prop", Global = true, User = true, FeatureEnum = NetworkedFeature.AllowPinning)]
        AllowWorldPropPinning = 0x1007,

        [Access(Category = "SettingsPet", Name = "Allow Movement Controls", DescriptionGlobal = "Allows all masters to disable Flight and Seat usage, may include more down the road.", DescriptionUser = "Allows this master to disable Flight and Seat usage, may include more down the road.", Global = true, User = true, FeatureEnum = NetworkedFeature.DisableFlight)]
        AllowMovementControls = 0x1008,
        [Access(Category = "SettingsPet", Name = "Allow Blindfolding", DescriptionGlobal = "Allows all masters to blindfold you!", DescriptionUser = "Allows this master to blindfold you!", Global = true, User = true, FeatureEnum = NetworkedFeature.AllowBlindfolding)]
        AllowBlindfolding = 0x1009,
        [Access(Category = "SettingsPet", Name = "Allow Deafening", DescriptionGlobal = "Allows all masters to deafen you!", DescriptionUser = "Allows this master to deafen you!", Global = true, User = true, FeatureEnum = NetworkedFeature.AllowDeafening)]
        AllowDeafening = 0x1010,
        [Access(Category = "SettingsPet", Name = "Allow Switching to Any Avatar", DescriptionGlobal = "Allows all masters to change you into ANY avatar that you can access, you should be mindful of who can do this!", DescriptionUser = "Allows this master to change you into ANY avatar that you can access, you should be mindful of who can do this!", Global = true, User = true, FeatureEnum = NetworkedFeature.AllowAnyAvatarSwitching, ConfirmPrompt = true, DialogMessageGlobal = "Enabling this will allow ANY master to change you into ANY avatar! Please be careful enabling this in instances where NSFW content isn't allowed!", DialogMessageUser = "Enabling this will allow this master to change you into ANY avatar! Please be careful about allowing usage of this in instances where NSFW content isn't allowed!")]
        AllowAnyAvatarSwitch = 0x1011,

        [Access(Category = "SettingsMaster", Name = "Allow Pet to follow you",
            DescriptionGlobal = "This will allow all pets to follow you on World change",
            DescriptionUser = "This will allow this pet to follow you on World change",
            Global = true, User = false,
            DialogMessageGlobal = "Send World Change to Pet will send enough information for your pet to join your instance, this applies to any instance type.")]
        AllowPetWorldChangeFollow = 0x2002,

        [Access(Category = "SettingsMaster", Name = "Auto Accept Master Requests",
            DescriptionGlobal = "This will allow all pets to make you their master automatically",
            DescriptionUser = "This will allow this pet to make you their master automatically",
            Global = true, User = true,
            DialogMessageGlobal = "Are you sure you want to enable this? This will allow ANYONE to make you their master!",
            DialogMessageUser = "Are you sure you want to enable this? This will allow this user to make you their master without confirmation!",
            ConfirmPrompt = true)]
        AutoAcceptMasterRequest = 0x2003,




        [Access(Category = "SettingsShock", Name = "Allow Shock Control",
            DescriptionGlobal = "Enables the PiShock Module for all Masters",
            DescriptionUser = "Enables the PiShock Module for this Master",
            Global = true, User = true)]
        AllowShockControl = 0x3001,

        [Access(Category = "SettingsShock", Name = "Allow Beep",
            DescriptionGlobal = "Allow the Beep Command for all Masters",
            DescriptionUser = "Allow the Beep Command for this Master",
            Global = true, User = true, FeatureEnum = NetworkedFeature.AllowBeep)]
        AllowBeep = 0x3002,

        [Access(Category = "SettingsShock", Name = "Allow Vibrate",
            DescriptionGlobal = "Allow the Vibrate Command for all Masters",
            DescriptionUser = "Allow the Vibrate Command for this Master",
            Global = true, User = true, FeatureEnum = NetworkedFeature.AllowVibrate)]
        AllowVibrate = 0x3003,

        [Access(Category = "SettingsShock", Name = "Allow Shock",
            DescriptionGlobal = "Allow the Shock Command for all Masters",
            DescriptionUser = "Allow the Shock Command for this Master",
            Global = true, User = true, FeatureEnum = NetworkedFeature.AllowShock)]
        AllowShock = 0x3004,

        [Access(Category = "SettingsShock", Name = "Allow Height Control",
            DescriptionGlobal = "Allows the Height Control for all Masters",
            DescriptionUser = "Allows the Height Control for this Master",
            Global = true, User = true, FeatureEnum = NetworkedFeature.AllowHeight)]
        AllowHeightControl = 0x3005,

        [Access(Category = "SettingsShock", Name = "Height Control Warning",
            DescriptionGlobal = "Will warn(vibrate) the shocker first for all Masters",
            DescriptionUser = "Will warn(vibrate) the shocker first for this Master",
            Global = true, User = true, Default = true)]
        HeightControlWarning = 0x3006,

        [Access(Category = "SettingsShock", Name = "Random Shocker Mode", DescriptionGlobal = "Control if PiShock Manager uses a random shocker", DescriptionUser = "Control if PiShock Manager uses a random shocker for this master", Global = true, User = true, Default = true)]
        ShockRandomShocker = 0x3007,

        [Access(Category = "SettingsTWNet", Name = "Custom Leash Colour",
            DescriptionGlobal = "Use the custom leash colour",
            Global = true, User = false, Default = false)]
        UseCustomLeashColour = 0x4001,
        
        [Access(Category = "SettingsTWNet", Name = "Hide Custom Leash Style",
            DescriptionGlobal = "Hides the custom leash style on other users, you can do this individually for users as well!",
            Global = true, User = true, Default = false, DescriptionUser = "Hides the custom leash style for this user")]
        HideCustomLeashStyle = 0x4002,

        [Access(Category = "MenuAdjHideCat", Name = "Hide Shocker Elements",
            DescriptionGlobal = "Hide All Elements related to shockers",
            Global = true, User = false, Default = false)]
        HidePiShock = 0x9001,

        [Access(Category = "MenuAdjHideCat", Name = "Hide Toy Strength",
            DescriptionGlobal = "Hide Toy Strength Sliders",
            Global = true, User = false, Default = false)]
        HideToyIntegration = 0x9002,
    }

    public class AccessAttribute : Attribute
    {
        public string Name;
        public string DescriptionGlobal;
        public string DescriptionUser;
        public bool Global;
        public bool User;
        public string Category;
        //Display a message when toggling this access
        public string DialogMessageGlobal = null;
        public string DialogMessageUser = null;
        //Display a confirmation prompt when toggling this access
        public bool ConfirmPrompt = false;
        public bool Default = false;
        public NetworkedFeature FeatureEnum;
    }
}