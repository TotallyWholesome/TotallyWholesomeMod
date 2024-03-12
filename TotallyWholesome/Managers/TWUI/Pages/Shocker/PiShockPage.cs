#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BTKUILib;
using BTKUILib.UIObjects;
using TotallyWholesome.Managers.Shockers;
using TotallyWholesome.Managers.Shockers.PiShock;
using TotallyWholesome.Managers.Shockers.PiShock.Config;
using TotallyWholesome.Objects;
using TotallyWholesome.Utils;
using WholesomeLoader;

namespace TotallyWholesome.Managers.TWUI.Pages.Shocker;

public static class PiShockPage
{
    public static readonly Page Page = CreateNewPage();
    private static Category? _shockersCategory;

    private static Page CreateNewPage()
    {
        var page = Page.GetOrCreatePage("TotallyWholesome", "PiShock Management");

        var cat = page.AddCategory("Settings");

        var addShocker = cat.AddButton("Add Shocker", "Key", "Add a new shocker via share code");
        addShocker.OnPress += () =>
        {
            QuickMenuAPI.OpenKeyboard("Enter your PiShock Share Code", s =>
            {
                if (string.IsNullOrEmpty(s))
                {
                    QuickMenuAPI.ShowNotice("Failed", "Failed to add PiShock Shocker! No share code was provided!");
                    return;
                }

                TwTask.Run(AddShareCodeUi(s));
            });
        };
        var refreshInfo = cat.AddButton("Refresh Shocker Info", "Reload", "Refreshes shockers data from PiShock");
        refreshInfo.OnPress += () => TwTask.Run(PiShockManager.Instance.UpdateShockers()); 

        var switchPlatform = cat.AddButton("Switch Platform", "Exit", "Switch the platform of the shockers");
        switchPlatform.OnPress += () =>
        {
            ShockerManager.Instance.SelectPlatform(Config.ShockerPlatform.None);

            QuickMenuAPI.GoBack();
            PlatformSelection.Page.OpenPage();
        };



        return page;
    }

    public static void UpdateShockerInfo(IDictionary<string, PiShockerInfo> shockersInfos)
    {
        _shockersCategory?.Delete();
        _shockersCategory = Page.AddCategory("Shockers");
        
        foreach (var (key, value) in PiShockConfig.Config.Shockers)
        {
            var button =
                _shockersCategory.AddButton(key, value.Enabled ? "ToggleOn" : "ToggleOff", "Toggle Shocker Enablement");
            if (shockersInfos.TryGetValue(key, out var info))
            {
                button.ButtonText = info.Name;
            }
            button.OnPress += () =>
            {
                value.Enabled = !value.Enabled;
                button.ButtonIcon = value.Enabled ? "ToggleOn" : "ToggleOff";
                PiShockConfig.SaveFnF();
            };
        }
    }

    private static DateTime _lastRefresh;
    private static DateTime _lastLogsRefresh;

    private static void RefreshAll()
    {
        if(DateTime.UtcNow.Subtract(_lastRefresh).TotalSeconds < 10)
        {
            QuickMenuAPI.ShowNotice("Hold up!", "You've already refreshed your shockers! Please wait a bit before refreshing again!");
            return;
        }
        
        _lastRefresh = DateTime.UtcNow;

        TwTask.Run(PiShockManager.Instance.UpdateShockers());
    }
    
    private static async Task AddShareCodeUi(string code)
    {
        var result = await PiShockManager.Instance.AddShareCode(code);
        result.Switch(success => { QuickMenuAPI.ShowNotice("Success!", "Successfully added a new PiShock Shocker!"); },
            httpError =>
            {
                Con.Msg($"Failed to add PiShock Shocker! StatusCode: {httpError.StatusCode} Body: {httpError.Body}");
                QuickMenuAPI.ShowNotice("Failed",
                    "Failed to add PiShock Shocker! Please check that the share code is valid, if you are continuing to have issues please check the MelonLoader Console!");
            },
            error =>
            {
                QuickMenuAPI.ShowNotice("Failed",
                    "Failed to add PiShock Shocker! This seems to be a technical error. Please check that the share code is valid, if you are continuing to have issues please check the MelonLoader Console!");
            });
    }

    public static void LogsRefresh()
    {
        if (DateTime.UtcNow.Subtract(_lastLogsRefresh).TotalSeconds < 10)
        {
            QuickMenuAPI.ShowNotice("Hold up!",
                "You've already refreshed your shocker logs! Please wait a bit before refreshing again!");
            return;
        }

        _lastLogsRefresh = DateTime.UtcNow;
    }
}