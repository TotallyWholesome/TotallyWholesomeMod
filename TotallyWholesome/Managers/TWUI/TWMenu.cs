using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using BTKUILib.UIObjects.Objects;
using MelonLoader;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Shockers;
using TotallyWholesome.Managers.Shockers.OpenShock.Models.SignalR;
using TotallyWholesome.Managers.TWUI.Pages;
using TotallyWholesome.Network;
using TotallyWholesome.Notification;
using TotallyWholesome.Utils;
using TWNetCommon.Data.ControlPackets.Shockers.Models;
using TWNetCommon;
using TWNetCommon.Data;
using TWNetCommon.Data.ControlPackets;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome.Managers.TWUI;

public class TWMenu : ITWManager
{
    public static TWMenu Instance { get; private set; }
    
    public static Page TWRootPage { get; private set; }
    public static Dictionary<string, Page> Pages { get; private set; } = new();
    public static Dictionary<string, Category> Categories { get; private set; } = new();

    public static ToggleButton HeightControl;
    public static CustomElement TWMenuButton;
    public static CustomEngineOnFunction TWStatusUpdate;
    public static CustomEngineOnFunction TWStatusPosition;

    private static Button _masterIndicator;
    
    
    //for all pets
    public SliderFloat Strength;
    public SliderFloat Duration;
    public SliderFloat ShockHeight;
    public SliderFloat ShockHeightStrengthMin;
    public SliderFloat ShockHeightStrengthMax;
    public SliderFloat ShockHeightStrengthStep;

    public int Priority => 10;

    public void Setup()
    {
        Instance = this;
        
        RegIcon("Ability");
        RegIcon("Aim");
        RegIcon("Alert");
        RegIcon("ArrowLeft", "Arrow Left");
        RegIcon("BadgeBronze", "Badge-Bronze");
        RegIcon("BadgeGold", "Badge-Gold");
        RegIcon("BadgeSilver", "Badge-Silver");
        RegIcon("Body");
        RegIcon("Bolt");
        RegIcon("Checkmark");
        RegIcon("CrownStars", "Crown - Stars");
        RegIcon("DiscordLogoWhite", "Discord-Logo-White");
        RegIcon("Exit");
        RegIcon("Handcuffs");
        RegIcon("Key");
        RegIcon("Link");
        RegIcon("ListX3", "List x3");
        RegIcon("Megaphone");
        RegIcon("MicrophoneOff", "Microphone Off");
        RegIcon("Multiuser");
        RegIcon("Profile");
        RegIcon("Reload");
        RegIcon("Resize");
        RegIcon("Settings");
        RegIcon("SpecialMark");
        RegIcon("Star");
        RegIcon("TurnOff", "Turn Off");
        RegIcon("TWClose", "TW-Close");
        RegIcon("TWTrash", "TW-Trash");
        RegIcon("TWLogoPride", "TW_Logo_Pride-sm");
        RegIcon("TWTabIcon", "TW_TabIcon");
        RegIcon("UserPlusRight", "User - Plus Right");
        RegIcon("VolumeMax", "Volume - Maximum");
        RegIcon("Vibration");
        RegIcon("OpenShock");
        RegIcon("PiShock");
        RegIcon("ToggleOn");
        RegIcon("ToggleOff");
        RegIcon("ExternalLink");
        
        TWNetListener.PetConfigUpdateEvent += PetConfigUpdateEvent;
        LeadManager.OnFollowerPairCreated += OnFollowerPairCreated;
        LeadManager.OnFollowerPairDestroyed += OnFollowerPairDestroyed;
        QuickMenuAPI.OnOpenedPage += OnOpenedPage;
    }
    
    /// <summary>
    /// Register a QuickMenuAPI icon
    /// </summary>
    /// <param name="iconName">Name to use within the QuickMenu API. This also is the filename without the extension. Extension needs to be png</param>
    private void RegIcon(string iconName) => RegIcon(iconName, iconName);
    
    
    /// <summary>
    /// Register a QuickMenuAPI icon
    /// </summary>
    /// <param name="iconName">Name to use within the QuickMenu API</param>
    /// <param name="imageName">Just the filename without the extension. Extension needs to be png</param>
    private void RegIcon(string iconName, string imageName) => 
        QuickMenuAPI.PrepareIcon("TotallyWholesome", iconName,
            Assembly.GetExecutingAssembly().GetManifestResourceStream($"TotallyWholesome.Images.{imageName}.png"));
    

    private void OnFollowerPairDestroyed(LeadPair obj)
    {
        _masterIndicator.ButtonText = "No Master";
        _masterIndicator.ButtonTooltip = "You don't currently have a master!";
        _masterIndicator.ButtonIcon = "TWClose";
    }

    private void OnFollowerPairCreated(LeadPair pair)
    {
        _masterIndicator.ButtonText = $"Master: {pair.Master.Username}";
        _masterIndicator.ButtonTooltip = $"Your current master is {pair.Master.Username}, you can clear this leash by clicking here!";
        _masterIndicator.ButtonIcon = pair.Master.PlayerIconURL;
    }

    private void PetConfigUpdateEvent(PetConfigUpdate packet)
    {
        //If it's just a param update we ignore it here
        if (packet.UpdateType == UpdateType.RemoteParamUpdate) return;

        if (!LeadManager.Instance.ActiveLeadPairs.TryGetValue(packet.Key, out var leadPair)) return;

        Main.Instance.MainThreadQueue.Enqueue(() =>
        {
            if (packet.UpdateType.HasFlag(UpdateType.AvatarListUpdate))
            {
                leadPair.SwitchableAvatars = packet.AllowedAvatars;
                leadPair.UpdatedSwitchableAvatars = true;

                if (IndividualPetControl.Instance.SelectedLeadPair == leadPair)
                {
                    IndividualPetControl.Instance.UpdateAvatarSwitching(leadPair);
                    IndividualPetControl.Instance.UpdateButtonStates(leadPair.EnabledFeatures);
                }
            }

            if (packet.UpdateType.HasFlag(UpdateType.AllowedFeaturesUpdate))
            {
                leadPair.EnabledFeatures = packet.AllowedFeatures;
                if (IndividualPetControl.Instance.SelectedLeadPair == leadPair)
                {
                    IndividualPetControl.Instance.UpdateButtonStates(packet.AllowedFeatures);
                    PetRestrictionsPage.Instance.UpdateButtonStates(packet.AllowedFeatures);
                }
            }
        });
    }

    public void LateSetup()
    {
        Con.Msg("Adding TW elements to BTKUI!");

        //Let's setup a custom element?
        TWMenuButton = new CustomElement("""{"c": "container twUI-btn", "s": [{"c": "icon"}, {"c": "status-background hidden", "a":{"id": "twUI-status-bg"}}, {"c": "status-pet hidden", "a":{"id": "twUI-status-pet"}}, {"c": "status-master hidden", "a":{"id": "twUI-status-master"}}, {"c": "status-icon-shock hidden", "a":{"id": "twUI-status-icon-shock"}}, {"c": "status-icon-vibrate hidden", "a":{"id": "twUI-status-icon-vibrate"}}, {"c": "badge hidden", "a":{"id": "twUI-badge"}, "s":[{"c": "badge-text", "a":{"id": "twUI-badgeText"}, "h":"DEV"}]}], "x":"twUIAction-OpenTab", "a":{"id":"btkUI-Custom-[UUID]"}}""", ElementType.GlobalElement);
        //Inject CSS for the TW button?

        using (StreamReader stream = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("TotallyWholesome.Managers.TWUI.TWStyle.css") ?? throw new InvalidOperationException()))
        {
            string cssStyle = stream.ReadToEnd();
            QuickMenuAPI.InjectCSSStyle(cssStyle);
        }

        TWRootPage = new Page("TotallyWholesome", "TWRoot", true, null, null, true);

        //Button custom action, I hate this so much but it does work
        TWMenuButton.AddAction("twUIAction-OpenTab", $"uiRefBTK.core.playSoundCore(\"Click\");var tabs = document.querySelectorAll(\".container-tabs .tab\");for(let i=0; i < tabs.length; i++){{let tab = tabs[i];tab.classList.remove(\"selected\");}}engine.call(\"btkUI-TabChange\", \"{TWRootPage.ElementID}\");");

        TWStatusUpdate = new CustomEngineOnFunction("twStatusUpdate",
            """console.log("twStatusUpdate Fired | " + backgroundColour + " | " + textColour + " | " + badgeText + " | " + displayBadge + " | " + petAutoAccept + " | " + masterAutoAccept + " | " + vibDevice + " | " + shockDevice);if(displayBadge) {cvr("#twUI-badge").show();}else {cvr("#twUI-badge").hide();}if(petAutoAccept){cvr("#twUI-status-pet").show();}else {cvr("#twUI-status-pet").hide();}if(masterAutoAccept){cvr("#twUI-status-master").show();let masterAutoElement = document.getElementById("twUI-status-master");if(petAutoAccept){masterAutoElement.classList.add("status-master-mask");}else{masterAutoElement.classList.remove("status-master-mask");}}else {cvr("#twUI-status-master").hide();}if(vibDevice){cvr("#twUI-status-icon-vibrate").show();}else {cvr("#twUI-status-icon-vibrate").hide();}if(shockDevice){cvr("#twUI-status-icon-shock").show();}else {cvr("#twUI-status-icon-shock").hide();}if(shockDevice || vibDevice){cvr("#twUI-status-bg").show();}else{cvr("#twUI-status-bg").hide();}cvr("#twUI-badgeText").innerHTML(badgeText);document.getElementById('twUI-badge').setAttribute('style', 'background-color: ' + backgroundColour + ';');document.getElementById('twUI-badgeText').setAttribute('style', 'color: ' + textColour + ';');""",
            new Parameter("backgroundColour", typeof(string), true, false),
            new Parameter("textColour", typeof(string), true, false),
            new Parameter("badgeText", typeof(string), true, false),
            new Parameter("displayBadge", typeof(bool), true, false),
            new Parameter("petAutoAccept", typeof(bool), true, false),
            new Parameter("masterAutoAccept", typeof(bool), true, false),
            new Parameter("vibDevice", typeof(bool), true, false),
            new Parameter("shockDevice", typeof(bool),true , false));
        TWMenuButton.AddEngineOnFunction(TWStatusUpdate);

        TWStatusPosition = new CustomEngineOnFunction("twStatusPositionUpdate", """console.log("Logo Position Update Func: LogoYPos=" + logoYPos + " | LogoXPos=" + logoXPos); let btn = document.getElementById(UUID); btn.style.top = logoYPos + "px"; btn.style.left = logoXPos + "px";""",
            new Parameter("logoXPos", typeof(int), true, false),
            new Parameter("logoYPos", typeof(int), true, false),
            new Parameter("UUID", typeof(string), true, false));
        TWMenuButton.AddEngineOnFunction(TWStatusPosition);

        TWMenuButton.OnElementGenerated += () =>
        {
            TWStatusPosition.TriggerEvent(Configuration.JSONConfig.LogoPositionX, Configuration.JSONConfig.LogoPositionY, TWMenuButton.ElementID);
        };

    
        TWRootPage.MenuTitle = $"Totally Wholesome - {BuildInfo.AssemblyVersion} {(BuildInfo.isBetaBuild ? "Beta Build" : "Release Build")}";
    
        _initialised = true;
        UpdateMenuSubtitle();

        var twCategory = TWRootPage.AddCategory("My TW", true, false);

        Pages.Add("MyTW", twCategory.AddPage("Profile", "Profile", "Configure your Totally Wholesome status, view achievements, and other various functions!", "TotallyWholesome"));
        Pages.Add("AvatarRemoteConfig", twCategory.AddPage("Avatar Remote Config", "Body", "Choose what parameters your master can control", "TotallyWholesome"));
        Pages.Add("Settings", twCategory.AddPage("TW Settings", "Settings", "Adjust your Totally Wholesome settings", "TotallyWholesome"));
        var clearNotifs = twCategory.AddButton("Clear Notifications", "TWTrash", "Clear all pending HUD notifications");
        clearNotifs.OnPress += () =>
        {
            QuickMenuAPI.ShowAlertToast("Cleared HUD notifications!");
            NotificationSystem.ClearNotification();
        };

        //Create the pages we'll be needing
        var mainControls = TWRootPage.AddCategory("Controls", true);
        //Store the category for later usage
        Categories.Add("MainControls", mainControls);

        var removeLeashes = mainControls.AddButton("Remove Leashes", "TWClose", "Remove all leashes connected to you");
        removeLeashes.OnPress += LeadManager.ClearLeads;

        var openGlobalRestrictions = mainControls.AddButton("Restrictions", "Ability", "Access pet restrictions, movement, blindness, deafening, world pinning and more!");
        openGlobalRestrictions.OnPress += () => PetRestrictionsPage.Instance.OpenRestrictionsPage();

        Pages.Add("IPC", mainControls.AddPage("Individual Pet Controls", "Multiuser", "Control your pets settings individually", "TotallyWholesome"));

        _masterIndicator = mainControls.AddButton("No Master", "TWClose", "You don't have a master right now!", ButtonStyle.FullSizeImage);
        _masterIndicator.OnPress += () =>
        {
            if (LeadManager.Instance.MasterPair == null) return;

            QuickMenuAPI.ShowConfirm("Remove Leash?", $"This will remove the leash with {LeadManager.Instance.MasterPair.Master.Username}", () =>
            {
                TwTask.Run(TWNetClient.Instance.SendAsync(new LeadAccept()
                {
                    Key = LeadManager.Instance.MasterPair.Key,
                    LeadRemove = true
                }, TWNetMessageType.LeadAccept));

                QuickMenuAPI.ShowAlertToast($"Removed leash with {LeadManager.Instance.MasterPair.Master.Username}!");
            });
        };

        //Time for the main menu sliders
        LeadManager.Instance.TetherRange = mainControls.AddSlider("Leash Length", "Adjust the length of the leash for all pets", 2f, 0f, 10f, 1, 2f, true);
        ButtplugManager.Instance.ToyStrength = mainControls.AddSlider("Toy Strength", "Control the strength of all your pets toys", 0f, 0f, 1f);

        ButtplugManager.Instance.ToyStrength.Hidden = ConfigManager.Instance.IsActive(AccessType.HideToyIntegration);

        //PiShock main page
        var shockCategory = TWRootPage.AddCategory("Shock Controls");
        Categories.Add("MainShock", shockCategory);

        shockCategory.Hidden = ConfigManager.Instance.IsActive(AccessType.HidePiShock);

        var beep = shockCategory.AddButton("Beep", "VolumeMax", "Sends a beep of the set duration to all pets");
        beep.OnPress += () => ShockerAction(ControlType.Sound);

        var vibrate = shockCategory.AddButton("Vibrate", "Star", "Sends a vibration of the set duration and intensity to all pets");
        vibrate.OnPress += () => ShockerAction(ControlType.Vibrate);

        var shock = shockCategory.AddButton("Shock", "Bolt", "Sends a shock of the set duration and duration to all pets");
        shock.OnPress += () => ShockerAction(ControlType.Shock);

        HeightControl = shockCategory.AddToggle("Height Control", "Enable the height control system", false);
        HeightControl.OnValueUpdated += _ => HeightControlUpdate();

        Strength = shockCategory.AddSlider("Strength", "Sets the strength of the shocks/vibrations. Scales according to limits in share link.", 0f, 0f, 100f);
        Strength.OnValueUpdated += _ => ShockerUpdate();
        Duration = shockCategory.AddSlider("Duration", "Sets the duration of shocks/beeps/vibrations. Scales according to limits in share links", 0f, 0f, 15f);
        Duration.OnValueUpdated += _ => ShockerUpdate();
        
        ShockHeight = shockCategory.AddSlider("Shock Height", "Shocker Height Control Max Height allowed", 0f, 0f, 4f);
        ShockHeight.OnValueUpdated += _ => HeightControlUpdate();
        ShockHeightStrengthMin = shockCategory.AddSlider("Shock Height Min Strength", "Shocker Height Control Min Strength", 0f, 0f, 1f);
        ShockHeightStrengthMin.OnValueUpdated += _ => HeightControlUpdate();
        ShockHeightStrengthMax = shockCategory.AddSlider("Shock Height Max Strength", "Shocker Height Control Max Strength", 0f, 0f, 1f);
        ShockHeightStrengthMax.OnValueUpdated += _ => HeightControlUpdate();
        ShockHeightStrengthStep = shockCategory.AddSlider("Shock Height Step Increase", "Shocker Height Control Step Increase", 0f, 0f, 1f);
        ShockHeightStrengthStep.OnValueUpdated += _ => HeightControlUpdate();

        //Player select category
        var playerSelectCat = QuickMenuAPI.PlayerSelectPage.AddCategory("Totally Wholesome", "TotallyWholesome");
        var master = playerSelectCat.AddButton("Make Master", "CrownStars", "Sends a master request to the selected user");
        master.OnPress += LeadManager.RequestToBePet;
        var pet = playerSelectCat.AddButton("Make Pet", "Link", "Sends a pet request to the selected user");
        pet.OnPress += LeadManager.RequestToBeMaster;
        var manageUser = playerSelectCat.AddButton("Manage User", "ListX3", "Opens the individual user permissions page for this user");
        manageUser.OnPress += () =>
        {
            TWSettingsUI.Instance.OpenManageUser(QuickMenuAPI.SelectedPlayerID);
        };
        var remoteParam = playerSelectCat.AddButton("Remote Param Control", "ListX3", "Opens the remote parameter control for this user (if they're your pet)");
        remoteParam.OnPress += () =>
        {
            IndividualPetControl.Instance.OpenParamControl(QuickMenuAPI.SelectedPlayerID);
        };
        
    }

    #region Menu Subtitle

    private int _onlineUsers;
    
    public int OnlineUsers
    {
        get => _onlineUsers;
        set
        {
            _onlineUsers = value;
            UpdateMenuSubtitle();
        }
    }

    private SignalRStatus _signalROpenShockStatus;
    
    public SignalRStatus OpenShockStatus
    {
        get => _signalROpenShockStatus;
        set
        {
            _signalROpenShockStatus = value;
            UpdateMenuSubtitle();
        }
    }
    
    private bool _initialised;
    
    private void UpdateMenuSubtitle()
    {
        var sb = new StringBuilder();
        sb.Append("Connected Users: ");
        if(OnlineUsers < 0) sb.Append("Disconnected");else sb.Append(OnlineUsers);

        if (OpenShockStatus != SignalRStatus.Uninitialized)
        {
            sb.Append(" | OpenShock Status: ");
            sb.Append(OpenShockStatus.ToString());
        }

        var finalString = sb.ToString();
        
        Main.Instance.MainThreadQueue.Enqueue(() =>
        {
            if(!_initialised) return;
            TWRootPage.MenuSubtitle = finalString;
        });
    }
    
    #endregion

    private void OnOpenedPage(string target, string lastPage)
    {
        if(target != TWRootPage.ElementID) return;

        if (Configuration.JSONConfig.AcceptedTOS < Main.CurrentTOSLevel)
        {
            MelonCoroutines.Start(WaitBeforeOpeningEULAPrompt());
            return;
        }

        if (Configuration.JSONConfig.ShownUpdateNotice != 10)
        {
            MelonCoroutines.Start(WaitBeforeShowUpdateNotice());
            return;
        }

        if (!Configuration.JSONConfig.ShownDiscordNotice)
        {
            MelonCoroutines.Start(WaitBeforeOpeningDiscordNotice());
        }
    }

    #region Notices

    private static IEnumerator WaitBeforeOpeningEULAPrompt()
    {
        yield return new WaitForSeconds(.2f);

        QuickMenuAPI.ShowConfirm("Totally Wholesome EULA",
            "<p>Before you can connect to TWNet you must agree to our End User License Agreement!</p><p>You can read the EULA at<br/>https://wiki.totallywholeso.me/eula</p>", () =>
            {
                Configuration.JSONConfig.AcceptedTOS = Main.CurrentTOSLevel;
                Configuration.SaveConfig();

                TWNetClient.Instance.ConnectClient();
            }, () =>
            {
                QuickMenuAPI.ShowAlertToast("You've declined the EULA, you will not be able to use Totally Wholesome until you accept it!");
            },
            "Accept", "Decline");
    }

    private static IEnumerator WaitBeforeOpeningDiscordNotice()
    {
        yield return new WaitForSeconds(.2f);

        QuickMenuAPI.ShowNotice("Join Our Discord!",
            "<p>Thank you for using Totally Wholesome!</p><p>You can join our Discord to keep up to date on changes and such around Totally Wholesome!</p><p>You can find the invite in Status and ETC!</p>",
            () =>
            {
                Configuration.JSONConfig.ShownDiscordNotice = true;
                Configuration.SaveConfig();
            });
    }

    private static IEnumerator WaitBeforeShowUpdateNotice()
    {
        yield return new WaitForSeconds(.2f);

        QuickMenuAPI.ShowNotice("Welcome to TW 3.5!",
            "<p>Welcome to Totally Wholesome v3.5!</p><p>Been a while hasn't it, we've got some fun stuff in here! This update has been in progress for quite a while, nearly every part of TW has been touch in some way, we've got some new features too!</p><p>Changes:</p><p> - BTKUILib Menu Rework</p><p> - Shocker System Rework</p><p> - Remote Avatar Switching</p><p> - Massive internal fixes and reworks!</p><p>Check the TW Discord for more information!</p>", () =>
            {
                Configuration.JSONConfig.ShownUpdateNotice = 10;
                Configuration.SaveConfig();
            });
    }

    #endregion
    
    private void ShockerAction(ControlType type)
    {
        TwTask.Run(ShockerManager.Instance.SendControlNetworked(type, Convert.ToByte(Strength.SliderValue),
            Convert.ToUInt16(Duration.SliderValue * 1000)));

        QuickMenuAPI.ShowAlertToast(
            $"Sent {type.ToString()} to all your pets for {Duration.SliderValue} seconds at {Strength.SliderValue}%!");
    }
    
    private void ShockerUpdate()
    {
        var intensity = Convert.ToByte(Strength.SliderValue);
        var duration = Convert.ToUInt16(Duration.SliderValue * 1000);
        foreach (var instancePetPair in LeadManager.Instance.PetPairs)
        {
            instancePetPair.Shocker.Intensity = intensity;
            instancePetPair.Shocker.Duration = duration;
        }
    }

    private static readonly Color ColorShown = new Color(0, 1, 0, 0.3f);
    
    private void HeightControlUpdate()
    {
        foreach (var instancePetPair in LeadManager.Instance.PetPairs)
        {

            instancePetPair.Shocker.HeightControl.Enabled = HeightControl.ToggleValue;
            instancePetPair.Shocker.HeightControl.Height = ShockHeight.SliderValue;
            instancePetPair.Shocker.HeightControl.StrengthMin = ShockHeightStrengthMin.SliderValue;
            instancePetPair.Shocker.HeightControl.StrengthMax = ShockHeightStrengthMax.SliderValue;
            instancePetPair.Shocker.HeightControl.StrengthStep = ShockHeightStrengthStep.SliderValue;
        }

        TwTask.Run(TWNetSendHelpers.SendHeightControl(
            HeightControl.ToggleValue,
            ShockHeight.SliderValue,
            ShockHeightStrengthMin.SliderValue,
            ShockHeightStrengthMax.SliderValue,
            ShockHeightStrengthStep.SliderValue));
    }
}