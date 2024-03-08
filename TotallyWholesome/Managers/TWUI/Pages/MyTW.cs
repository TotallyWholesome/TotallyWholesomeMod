using System;
using System.Diagnostics;
using System.Linq;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using TotallyWholesome.Managers.Achievements;
using TotallyWholesome.Managers.Status;
using WholesomeLoader;

namespace TotallyWholesome.Managers.TWUI.Pages;

public class MyTW : ITWManager
{
    public static MyTW Instance;

    public int Priority => 5;

    public Category AchievementCategory;
    public ToggleButton EnableStatus, DisplayBadge, HideInPublic, ShowDeviceStatus, ShowAutoAccept;

    private Page _achievementPage;

    public void Setup()
    {
        Instance = this;

        QuickMenuAPI.OnOpenedPage += OnOpenedPage;
    }

    private void OnOpenedPage(string target, string last)
    {
        if (!target.Equals(_achievementPage.ElementID) || !AchievementManager.Instance.AchievementsUpdated) return;

        AchievementManager.Instance.AchievementsUpdated = false;

        AchievementCategory.ClearChildren();

        foreach (var achievement in AchievementManager.Instance.LoadedAchievements.OrderBy(x => x.AchievementName))
        {
            var button = AchievementCategory.AddButton(achievement.AchievementAwarded ? achievement.AchievementName : "???", "Badge"+ Enum.GetName(typeof(AchievementRank), achievement.AchievementRank),
                achievement.AchievementAwarded ? achievement.AchievementDescription : "You must unlock this achievement before you can see the description!");

            button.Disabled = !achievement.AchievementAwarded;
        }
    }

    public void LateSetup()
    {
        var page = TWMenu.Pages["MyTW"];

        var mainCat = page.AddCategory("Main", false, false);

        EnableStatus = mainCat.AddToggle("Enable Status", "Choose if you want your Totally Wholesome status to be broadcast to other users in your world", Configuration.JSONConfig.EnableStatus);
        EnableStatus.OnValueUpdated += b =>
        {
            Configuration.JSONConfig.EnableStatus = b;
            Configuration.SaveConfig();

            StatusManager.Instance.SendStatusUpdate();
        };

        DisplayBadge = mainCat.AddToggle("Display Special Badge", "Enables your special badge if you have one, if you've helped test TW you might have one", Configuration.JSONConfig.DisplaySpecialStatus);
        DisplayBadge.OnValueUpdated += b =>
        {
            Configuration.JSONConfig.DisplaySpecialStatus = b;
            Configuration.SaveConfig();

            StatusManager.Instance.SendStatusUpdate();
        };

        HideInPublic = mainCat.AddToggle("Hide Status in Publics", "Hides your TW status when you join public instances", Configuration.JSONConfig.HideInPublicWorlds);
        HideInPublic.OnValueUpdated += b =>
        {
            Configuration.JSONConfig.HideInPublicWorlds = b;
            Configuration.SaveConfig();

            StatusManager.Instance.SendStatusUpdate();
        };

        ShowDeviceStatus = mainCat.AddToggle("Show Device Status", "Display the status of your Buttplug.io/shocker devices", Configuration.JSONConfig.ShowDeviceStatus);
        ShowDeviceStatus.OnValueUpdated += b =>
        {
            Configuration.JSONConfig.ShowDeviceStatus = b;
            Configuration.SaveConfig();

            StatusManager.Instance.SendStatusUpdate();
        };

        ShowAutoAccept = mainCat.AddToggle("Show Auto Accept", "Toggle the visibility of your auto accept status", Configuration.JSONConfig.ShowAutoAccept);
        ShowAutoAccept.OnValueUpdated += b =>
        {
            Configuration.JSONConfig.ShowAutoAccept = b;
            Configuration.SaveConfig();

            StatusManager.Instance.SendStatusUpdate();
        };

        var enterKey = mainCat.AddButton("Enter Rank Key", "Key", "Enter the rank key given to you from beta testing to enable your badge");
        enterKey.OnPress += () =>
        {
            QuickMenuAPI.OpenKeyboard(Configuration.JSONConfig.LoginKey, s =>
            {
                Configuration.JSONConfig.LoginKey = s;
                Configuration.SaveConfig();

                Con.Debug($"LoginKey updated to {s}");
            });
        };

        _achievementPage = mainCat.AddPage("My Achievements", "BadgeGold", "View the achievements you've earned!", "TotallyWholesome");
        AchievementCategory = _achievementPage.AddCategory("Main", false);

        var miscCat = page.AddCategory("Misc", true, false);
        var discordLink = miscCat.AddButton("Join Our Discord!", "DiscordLogoWhite", "Opens up the invite link to the Totally Wholesome Discord!");
        discordLink.OnPress += () =>
        {
            Process.Start("https://discord.gg/GbyjZYVEEx");
        };

        var openLicences = miscCat.AddButton("Licences", "Link", "Click here to go to the 3rd party library licences page");
        openLicences.OnPress += () =>
        {
            Process.Start("https://wiki.totallywholeso.me/third-party-licenses");
        };

        var openEULA = miscCat.AddButton("EULA", "Link", "Click here to view the Totally Wholesome EULA");
        openEULA.OnPress += () =>
        {
            Process.Start("https://wiki.totallywholeso.me/eula");
        };
    }
}