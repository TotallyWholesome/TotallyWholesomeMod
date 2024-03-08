using BTKUILib;
using BTKUILib.UIObjects;
using TotallyWholesome.Managers.Shockers;
using TotallyWholesome.Objects;

namespace TotallyWholesome.Managers.TWUI.Pages.Shocker;

public static class PlatformSelection
{
    public static readonly Page Page = CreateNewPage();

    private static Page CreateNewPage()
    {
        var page = Page.GetOrCreatePage("TotallyWholesome", "ShockPlatformSelection");

        var cat = page.AddCategory("Select your Platform", true, false);

        var openShock = cat.AddButton("OpenShock", "OpenShock", "https://openshock.org");
        openShock.OnPress += () =>
        {
            ShockerManager.Instance.SelectPlatform(Config.ShockerPlatform.OpenShock);
            
            QuickMenuAPI.GoBack();
            OpenShockPage.Page.OpenPage();
        };
        
        var piShock = cat.AddButton("PiShock", "PiShock", "https://pishock.com");
        piShock.OnPress += () =>
        {
            ShockerManager.Instance.SelectPlatform(Config.ShockerPlatform.PiShock);
            
            QuickMenuAPI.GoBack();
            PiShockPage.Page.OpenPage();
        };
        
        return page;
    }
}