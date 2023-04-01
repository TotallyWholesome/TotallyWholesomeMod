using System;
using System.Collections.Generic;
using System.IO;
using ABI_RC.Core.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TotallyWholesome.Managers.Status;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using TotallyWholesome.TWUI;
using WholesomeLoader;

namespace TotallyWholesome.Managers
{
    public class ConfigManager
    {
        public static ConfigManager Instance;

        private Dictionary<AccessType, bool> _globalEntries; // per perm
        private Dictionary<string, List<AccessType>> _userEntries; // per user
        private string _settingsPathGlobal = Path.Combine(Configuration.SettingsPath, "GlobalSetttings.json");
        private string _settingsPathUser = Path.Combine(Configuration.SettingsPath, "UserSettings.json");

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

            SaveToFile();

            Con.Msg($"Loaded {_userEntries.Count} user Settings sets and {_globalEntries.Count} global Settings!");
        }

        [UIEventHandler("reloadConfig")]
        public static void ReloadConfig()
        {
            Configuration.Initialize();
            Instance.Setup();
        }

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

        public void SetActive(AccessType type, bool value, string toggleID, string userId = null)
        {
            var attribute = type.GetAttributeOfType<AccessAttribute>();

            if (attribute != null && value != attribute.Default)
            {
                bool shownPopup = false;
                
                //If value is set to true and confirmprompt is set show dialog
                if (attribute.ConfirmPrompt && toggleID != null)
                {
                    UIUtils.ShowConfirm(attribute.Name, userId == null ? attribute.DialogMessageGlobal : attribute.DialogMessageUser, "Confirm", null, "Cancel", () =>
                    {
                        SetActive(type, attribute.Default, toggleID, userId);
                        UIUtils.SetToggleState(type.ToString(), attribute.Default, attribute.Category, userId == null ? "Settings" : "UserPerms");
                    });

                    shownPopup = true;
                }

                if (attribute.DialogMessageGlobal != null && userId == null && !shownPopup)
                {
                    UIUtils.ShowNotice("Notice", attribute.DialogMessageGlobal);
                }

                if (attribute.DialogMessageUser != null && userId != null && !shownPopup)
                {
                    UIUtils.ShowNotice("Notice", attribute.DialogMessageUser);
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
            }
            else
            {
                _globalEntries[type] = value;
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
                case AccessType.EnableMuffledMode:
                    if (!value)
                        Audio.SetMicrophoneActive(true);
                    break;
                case AccessType.UseOldHudMessage:
                    NotificationSystem.UseCVRNotificationSystem = value;
                    break;
                case AccessType.AllowHeightControl:
                    PiShockManager.Instance.Reset();
                    break;
                case AccessType.HideToyIntegration:
                case AccessType.HidePiShock:
                    UIUtils.SendModInit();
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
    public enum AccessType
    {
        [Access(Category = "General", Name = "Hide the leash",
            DescriptionGlobal = "This will hide the leash for all your Pets/Masters",
            DescriptionUser = "This will hide the leash for this your Pets/Masters",
            Global = true, User = true,
            DialogMessageGlobal = "Changing No Visible Leash will not apply until you clear and create new leashes!",
            DialogMessageUser = "Changing No Visible Leash will not apply until you clear and create it again!")]
        NoVisibleLeash = 0x0001,

        [Access(Category = "General", Name = "Private Leash",
            DescriptionGlobal = "Only you and your Pets/Masters will see the Leash",
            DescriptionUser = "Only you and this Pets/Masters will see the Leash",
            Global = true, User = true,
            DialogMessageGlobal = "Changing Private Leash will not apply until you clear and create new leashes!",
            DialogMessageUser = "Changing Private Leash will not apply until you clear create it again!")]
        PrivateLeash = 0x0002,

        [Access(Category = "General", Name = "Auto Accept Requests from Friends only",
            DescriptionGlobal = "Only people you have friended will be allowed to make your their Pet/Master",
            Global = true, User = false)]
        AutoAcceptFriendsOnly = 0x0003,

        [Access(Category = "General", Name = "Block User",
            DescriptionUser = "Block this user from interacting with you",
            Global = false, User = true)]
        BlockUser = 0x0004,

        /*[Access(Category = "General", Name = "Use Tab Menu",
            DescriptionGlobal = "Toggles if Totally Wholesome uses a tab or a button in the launchpad",
            Global = true, User = false, Default = true)]
        UseTabMenu = 0x0005,*/

        [Access(Category = "General", Name = "Disable TW Notification System",
            DescriptionGlobal = "Disables the TW Notification system, if available it will use the NotificationAPI mod otherwise it'll use the CVR hud notifications",
            Global = true, User = false,
            DialogMessageGlobal = "Are you sure you want to disable the TW notification system? Disabling it will make TW use either the CVR hud notifications or the NotificationAPI mod if available.",
            ConfirmPrompt = true)]
        UseOldHudMessage = 0x0006,

        [Access(Category = "General", Name = "Pet/Master Join Notifications",
            DescriptionGlobal = "Shows a popup notification when your master or pet joins your current instance",
            Global = true, User = false, Default = true)]
        MasterPetJoinNotification = 0x0007,

        [Access(Category = "General", Name = "Use ActionMenu Controls",
            DescriptionGlobal = "Toggles the use of some pet controls in your Action Menu",
            Global = true, User = false, Default = true)]
        UseActionMenu = 0x0008,
        
        [Access(Category = "General", Name = "Hide TW Nameplate Badges", DescriptionGlobal = "Hides the TW nameplate badge that is seen on other TW users", Global = true, Default = false)]
        HideNameplateBadges = 0x0009,

        [Access(Category = "Pet", Name = "Auto Accept Pet Request",
            DescriptionGlobal = "This will allow ANYONE to make you their pet automatically",
            DescriptionUser = "This will allow this user to make you their pet automatically",
            Global = true, User = true,
            DialogMessageGlobal = "Are you sure you want to enable this? This will allow ANYONE to make you their pet!",
            DialogMessageUser = "Are you sure you want to enable this? This will allow this user to make you their pet without confirmation!",
            ConfirmPrompt = true)]
        AutoAcceptPetRequest = 0x1001,

        [Access(Category = "Pet", Name = "Allow Force Mute",
            DescriptionGlobal = "This will allow all masters to mute you",
            DescriptionUser = "This will allow this masters to mute you",
            Global = true, User = true,
            DialogMessageGlobal = "Allowing Force Mute will allow your master to mute you at any time! You can disable this setting to unlock it.",
            DialogMessageUser = "Allowing Force Mute will allow your master to mute you at any time! You can disable this setting to unlock it.")]
        AllowForceMute = 0x1002,

        [Access(Category = "Pet", Name = "Enable Muffled Mode",
            DescriptionGlobal = "Instead of muted you will be muffled",
            Global = true, User = false)]
        EnableMuffledMode = 0x1003,

        [Access(Category = "Pet", Name = "Enable Toy Control",
            DescriptionGlobal = "Enable Buttplug (Requires Restart)",
            Global = true, User = false)]
        EnableToyControl = 0x1004,

        [Access(Category = "Pet", Name = "Allow Toy Control",
            DescriptionGlobal = "This will allow all masters to control your Toys",
            DescriptionUser = "This will allow this masters to control your Toys",
            Global = true, User = true,
            DialogMessageGlobal = "Allowing Toy Control will allow your master to control your connected toys! You can disable this setting to reset it.",
            DialogMessageUser = "Allowing Toy Control will allow your master to control your connected toys! You can disable this setting to reset it.")]
        AllowToyControl = 0x1005,

        [Access(Category = "Pet", Name = "Follow Master on World Change",
            DescriptionGlobal = "This will make you follow all masters to the new World",
            DescriptionUser = "This will make you follow this master to the new World",
            Global = true, User = true,
            DialogMessageGlobal = "Follow Master World Changes will automatically follow your master to their new instance if they have it enabled, you will have 10 seconds before leaving!",
            DialogMessageUser = "Follow Master World Changes will automatically follow your master to their new instance if they have it enabled, you will have 10 seconds before leaving!")]
        FollowMasterWorldChange = 0x1006,
        
        [Access(Category = "Pet", Name = "Allow World/Prop Pinning", DescriptionGlobal = "Allows all masters to pin your leash to a part of the world or to a prop", DescriptionUser = "Allows this master to pin your leash to part of the world or to a prop", Global = true, User = true)]
        AllowWorldPropPinning = 0x1007,

        [Access(Category = "Pet", Name = "Allow Movement Controls", DescriptionGlobal = "Allows all masters to disable Flight and Seat usage, may include more down the road.", DescriptionUser = "Allows this master to disable Flight and Seat usage, may include more down the road.", Global = true, User = true)]
        AllowMovementControls = 0x1008,
        [Access(Category = "Pet", Name = "Allow Blindfolding", DescriptionGlobal = "Allows all masters to blindfold you!", DescriptionUser = "Allows this master to blindfold you!", Global = true, User = true)]
        AllowBlindfolding = 0x1009,
        [Access(Category = "Pet", Name = "Allow Deafening", DescriptionGlobal = "Allows all masters to deafen you!", DescriptionUser = "Allows this master to deafen you!", Global = true, User = true)]
        AllowDeafening = 0x1010,

        [Access(Category = "Master", Name = "Allow Pet to follow you",
            DescriptionGlobal = "This will allow all pets to follow you on World change",
            DescriptionUser = "This will allow this pets to follow you on World change",
            Global = true, User = false,
            DialogMessageGlobal = "Send World Change to Pet will send enough information for your pet to join your instance, this applies to any instance type.")]
        AllowPetWorldChangeFollow = 0x2002,

        [Access(Category = "Master", Name = "Auto Accept Master Requests",
            DescriptionGlobal = "This will allow all pets to make you their master automatically",
            DescriptionUser = "This will allow this pets to make you their master automatically",
            Global = true, User = true,
            DialogMessageGlobal = "Are you sure you want to enable this? This will allow ANYONE to make you their master!",
            DialogMessageUser = "Are you sure you want to enable this? This will allow this user to make you their master without confirmation!",
            ConfirmPrompt = true)]
        AutoAcceptMasterRequest = 0x2003,




        [Access(Category = "PiShock", Name = "Allow Shock Control",
            DescriptionGlobal = "Enables the PiShock Module for all Masters",
            DescriptionUser = "Enables the PiShock Module for this Master",
            Global = true, User = true)]
        AllowShockControl = 0x3001,

        [Access(Category = "PiShock", Name = "Allow Beep",
            DescriptionGlobal = "Allow the Beep Command for all Masters",
            DescriptionUser = "Allow the Beep Command for this Master",
            Global = true, User = true)]
        AllowBeep = 0x3002,

        [Access(Category = "PiShock", Name = "Allow Vibrate",
            DescriptionGlobal = "Allow the Vibrate Command for all Masters",
            DescriptionUser = "Allow the Vibrate Command for this Master",
            Global = true, User = true)]
        AllowVibrate = 0x3003,

        [Access(Category = "PiShock", Name = "Allow Shock",
            DescriptionGlobal = "Allow the Shock Command for all Masters",
            DescriptionUser = "Allow the Shock Command for this Master",
            Global = true, User = true)]
        AllowShock = 0x3004,

        [Access(Category = "PiShock", Name = "Allow Height Control",
            DescriptionGlobal = "Allows the Height Control for all Masters",
            DescriptionUser = "Allows the Height Control for this Master",
            Global = true, User = true)]
        AllowHeightControl = 0x3005,

        [Access(Category = "PiShock", Name = "Height Control Warning",
            DescriptionGlobal = "Will warn(vibrate) the shocker first for all Masters",
            DescriptionUser = "Will warn(vibrate) the shocker first for this Master",
            Global = true, User = true, Default = true)]
        HeightControlWarning = 0x3006,

        [Access(Category = "PiShock", Name = "Random Shocker Mode", DescriptionGlobal = "Control if PiShock Manager uses a random shocker", DescriptionUser = "Control if PiShock Manager uses a random shocker for this master", Global = true, User = true, Default = true)]
        PiShockRandomShocker = 0x3007,

        [Access(Category = "TWNet", Name = "Custom Leash Colour",
            DescriptionGlobal = "Use the custom leash colour",
            Global = true, User = false, Default = false)]
        UseCustomLeashColour = 0x4001,
        
        [Access(Category = "TWNet", Name = "Hide Custom Leash Style",
            DescriptionGlobal = "Hides the custom leash style on other users, you can do this individually for users as well!",
            Global = true, User = true, Default = false, DescriptionUser = "Hides the custom leash style for this user")]
        HideCustomLeashStyle = 0x4002,

        [Access(Category = "ToyControls", Name = "Hide Pi Shock Elements",
            DescriptionGlobal = "Hide All Elements related to PiShock",
            Global = true, User = false, Default = false)]
        HidePiShock = 0x9001,

        [Access(Category = "ToyControls", Name = "Hide Toy Strength",
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
    }
}