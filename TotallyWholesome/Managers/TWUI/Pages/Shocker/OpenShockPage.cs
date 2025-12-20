using System;
using System.Collections.Generic;
using ABI_RC.Systems.UI.UILib;
using ABI_RC.Systems.UI.UILib.UIObjects;
using ABI_RC.Systems.UI.UILib.UIObjects.Components;
using TotallyWholesome.Managers.Shockers;
using TotallyWholesome.Managers.Shockers.OpenShock;
using TotallyWholesome.Managers.Shockers.OpenShock.Config;
using TotallyWholesome.Managers.Shockers.OpenShock.Models;
using TotallyWholesome.Objects;
using UnityEngine;

namespace TotallyWholesome.Managers.TWUI.Pages.Shocker;

public static class OpenShockPage
{
    public static readonly Page Page = CreateNewPage();
    private static readonly IDictionary<Guid, Category> DeviceCategories = new Dictionary<Guid, Category>();

    private static Page CreateNewPage()
    {
        var page = Page.GetOrCreatePage("TotallyWholesome", "OpenShock Management");

        var cat = page.AddCategory("Settings", true, true);

        var openShockWebsite = cat.AddButton("OpenShock Website", "OpenShock", "Open the OpenShock website (OpenShock.org)");
        openShockWebsite.OnPress += () =>
        {
            Application.OpenURL("https://openshock.org");
        };
        
        var tokenButton = cat.AddButton("Token", "Key", "Set the OpenShock API token");
        tokenButton.OnPress += () =>
        {
            QuickMenuAPI.OpenKeyboard(OpenShockConfig.Config.ApiToken ?? string.Empty, s =>
            {
                OpenShockConfig.Config.ApiToken = s;
                OpenShockConfig.SaveFnF();
                OpenShockManager.Instance?.SetupServiceConnectionFnf();
            });
        };
        
        var serverButton = cat.AddButton("Server", "Link", "Set the OpenShock API server");
        serverButton.OnPress += () =>
        {
            QuickMenuAPI.OpenKeyboard(OpenShockConfig.Config.ApiBaseUrl.ToString(), s =>
            {
                if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
                {
                    QuickMenuAPI.ShowAlertToast("Failed to set server, uri is invalid");
                }
                OpenShockConfig.Config.ApiBaseUrl = uri;
                OpenShockConfig.SaveFnF();
                OpenShockManager.Instance?.SetupServiceConnectionFnf();
            });
        };
        
        var switchPlatform = cat.AddButton("Switch Platform", "Exit", "Switch the platform of the shockers");
        switchPlatform.OnPress += () =>
        {
            ShockerManager.Instance.SelectPlatform(Config.ShockerPlatform.None);
            
            QuickMenuAPI.GoBack();
            PlatformSelection.Page.OpenPage();
        };
        
        return page;
    }

    public static void UpdateDevices(IEnumerable<ResponseHubWithShockers> devices)
    {
        if (QuickMenuAPI.CurrentPageID == ShockerPage.Page.ElementID) QuickMenuAPI.GoBack();
        
        foreach (var (_, value) in DeviceCategories) value.Delete();
        DeviceCategories.Clear();

        foreach (var device in devices)
        {
            var cat = Page.AddCategory($"Device: {device.Name}", true, true);
            
            
            foreach (var shocker in device.Shockers)
            {
                var enabled = OpenShockConfig.Config.Shockers.TryGetValue(shocker.Id, out var shockerConfig) && shockerConfig.Enabled;
                var shockerButton = cat.AddButton(shocker.Name, enabled ? "ToggleOn" : "ToggleOff", "Open Shocker Settings", ButtonStyle.TextWithIcon);
                shockerButton.OnPress += () =>
                {
                    if(!OpenShockConfig.Config.Shockers.TryGetValue(shocker.Id, out var shockerConfigPress)) return;
                    ShockerPage.SetShockerConfig(shockerConfigPress, shockerButton, shocker);
                    ShockerPage.Page.OpenPage();
                };

            }
                
            DeviceCategories[device.Id] = cat;
        }
    }
}