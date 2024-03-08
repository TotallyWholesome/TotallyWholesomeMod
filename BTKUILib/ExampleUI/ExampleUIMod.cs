using System.Reflection;
using BTKUILib.UIObjects;
using MelonLoader;

namespace BTKUILib.ExampleUI;

public class ExampleUIMod : MelonMod
{
    private Page _exampleRootPage;
    
    public override void OnInitializeMelon()
    {
        QuickMenuAPI.PrepareIcon("OurMod", "BTKIcon", Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKUILib.ExampleUI.BTKIcon.png"));
        
        //This creates our root page AND the tab
        _exampleRootPage = new Page("OurMod", "Example Root", true, "BTKIcon");
        //This sets the title that appears at the very top in the header bar
        _exampleRootPage.MenuTitle = "Example UI";
        //This sets the subtitle that appears in the header bar
        _exampleRootPage.MenuSubtitle = "This is a subtitle!";
        
        //Let's make some elements!

        //This section creates our category and adds some simple elements to it
        var category = _exampleRootPage.AddCategory("Our Category");
        var button = category.AddButton("Test Button", "", "This is a test button!");
        button.OnPress += () =>
        {
            LoggerInstance.Msg("You clicked a button!");
            //Let's pop up a notice!
            QuickMenuAPI.ShowNotice("Notice!", "Notice me!");
        };

        var toggle = category.AddToggle("Toggle Time!", "Click the toggle to toggle a thing!", false);
        toggle.OnValueUpdated += b =>
        {
            LoggerInstance.Msg($"Our toggle just went {b}");
        };
        
        //How about a sub page?

        var subPage = category.AddPage("This is a subpage!", "", "Click here to open another page!", "OurMod");
        //Now we can do the same as above, or whatever else!
        
        //Sliders?
        subPage.AddSlider("Slider Time", "This is a slider!", 5f, 0f, 10f);
        
        
    }
}