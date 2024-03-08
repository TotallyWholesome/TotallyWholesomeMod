using System;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using TotallyWholesome.Managers.Shockers.OpenShock.Config;
using TotallyWholesome.Managers.Shockers.OpenShock.Models;
using TotallyWholesome.Managers.Status;

namespace TotallyWholesome.Managers.TWUI.Pages.Shocker;

public static class ShockerPage
{
    public static readonly Page Page = CreateNewPage();
    private static OpenShockConfig.OpenShockConf.ShockerConfig _shockerConfig;
    private static Button _button;
    
    private static Button _enabledToggle;
    private static ToggleButton _permShock;
    private static ToggleButton _permVibrate;
    private static ToggleButton _permSound;
    
    private static SliderFloat _intensityLimit;
    private static SliderFloat _durationLimit;

    static ShockerPage()
    {
        QuickMenuAPI.OnBackAction += PageChange;
        QuickMenuAPI.OnOpenedPage += PageChange;
    }
    
    private static void PageChange(string target, string previous)
    {
        if (previous == Page.ElementID) OpenShockConfig.SaveFnF();
    }

    private static Page CreateNewPage()
    {
        var shockerPage = Page.GetOrCreatePage("TotallyWholesome", "Shocker: Not Initialized");

        var shockerCategory = shockerPage.AddCategory(string.Empty, false, false);

        _enabledToggle = shockerCategory.AddButton("Enabled", "ToggleOff", "Enable or disable this shocker");
        _enabledToggle.OnPress += () =>
        {
            _shockerConfig.Enabled = !_shockerConfig.Enabled;
            _enabledToggle.ButtonIcon = _button.ButtonIcon = _shockerConfig.Enabled ? "ToggleOn" : "ToggleOff";
            
            StatusManager.Instance.DeviceChangeStatusUpdate();
        };

        var shockerPermsAndLimits = shockerPage.AddCategory(string.Empty, false, false);

        _permShock = shockerPermsAndLimits.AddToggle("Shock", "Allow shocking", false);
        _permShock.OnValueUpdated += b => _shockerConfig.AllowShock = b;

        _permVibrate = shockerPermsAndLimits.AddToggle("Vibrate", "Allow vibrating", false);
        _permVibrate.OnValueUpdated += b => _shockerConfig.AllowVibrate = b;

        _permSound = shockerPermsAndLimits.AddToggle("Beep", "Allow beeping", false);
        _permSound.OnValueUpdated += b => _shockerConfig.AllowSound = b;

        _intensityLimit = shockerPermsAndLimits.AddSlider("Intensity Limit",
            "The maximum intensity this shocker can go to",
            0, 0, 100);
        _intensityLimit.OnValueUpdated += f => _shockerConfig.LimitIntensity = Convert.ToByte(f);

        _durationLimit = shockerPermsAndLimits.AddSlider("Duration Limit",
            "The maximum intensity this shocker can go to",
            0, 0, 15);
        _durationLimit.OnValueUpdated += f => _shockerConfig.LimitDuration = Convert.ToUInt16(f * 1000);

        return shockerPage;
    }

    public static void SetShockerConfig(OpenShockConfig.OpenShockConf.ShockerConfig config, Button buttonRef,
        ShockerResponse shocker)
    {
        _shockerConfig = config;
        _button = buttonRef;
        
        Page.PageDisplayName = $"Shocker: {shocker.Name}";
        
        _enabledToggle.ButtonIcon = config.Enabled ? "ToggleOn" : "ToggleOff";
        _permShock.ToggleValue = config.AllowShock;
        _permVibrate.ToggleValue = config.AllowVibrate;
        _permSound.ToggleValue = config.AllowSound;
        _intensityLimit.SetSliderValue(config.LimitIntensity);
        _durationLimit.SetSliderValue(config.LimitDuration / 1000f);
    }
}