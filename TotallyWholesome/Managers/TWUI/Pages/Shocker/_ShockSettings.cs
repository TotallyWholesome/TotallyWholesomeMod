using ABI_RC.Systems.UI.UILib.UIObjects;
using TotallyWholesome.Objects;

namespace TotallyWholesome.Managers.TWUI.Pages.Shocker;

public class _ShockSettings : ITWManager
{
    int ITWManager.Priority => 5;

    public void Setup()
    {
        
    }

    public void LateSetup()
    {
        var shockCat = TWMenu.Categories["SettingsShock"];
        var settingButton = shockCat.AddButton("Shock Settings", "Bolt", "Configure settings for shockers");
        settingButton.OnPress += () =>
        {
            switch (Configuration.JSONConfig.SelectedShockerPlatform)
            {
                case Config.ShockerPlatform.None:
                    PlatformSelection.Page.OpenPage();
                    break;
                case Config.ShockerPlatform.OpenShock:
                    OpenShockPage.Page.OpenPage();
                    break;
                case Config.ShockerPlatform.PiShock:
                    PiShockPage.Page.OpenPage();
                    break;
            }
        };
    }
}