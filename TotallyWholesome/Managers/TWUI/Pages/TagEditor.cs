using ABI_RC.Core.InteractionSystem;
using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using TotallyWholesome.Network;
using TotallyWholesome.Utils;
using TWNetCommon;
using TWNetCommon.Data;
using WholesomeLoader;

namespace TotallyWholesome.Managers.TWUI.Pages;

public class TagEditor : ITWManager
{
    public static TagEditor Instance;

    public int Priority => 2;

    internal bool CanUseTag;

    //Page elements
    private Page _tagEditPage;
    private Category _tagPreviewCategory;

    private string _lastTagText;
    private string _lastTextColour;
    private string _lastBGColour;
    private bool _modEditor;
    private TagData _currentTagData;

    

    public void Setup()
    {
        Instance = this;

        
    }

    

    public void LateSetup()
    {
        //Create a non child root page
        _tagEditPage = Page.GetOrCreatePage("TotallyWholesome", "Tag Editor");
        QuickMenuAPI.AddRootPage(_tagEditPage);

        _tagEditPage.PageDisplayName = "Tag Editor";

        _tagPreviewCategory = _tagEditPage.AddCategory("TAG", true);
        var editText = _tagPreviewCategory.AddButton("Edit Tag Text", "Profile", "Edit tag text");
        editText.OnPress += EditTagText;

        var editBGColour = _tagPreviewCategory.AddButton("Edit Background Colour", "Ranking", "Edit tag background colour");
        var editTextColour = _tagPreviewCategory.AddButton("Edit Text Colour", "Ranking", "Edit tag text colour");

        var actionCat = _tagEditPage.AddCategory("Actions");
        var save = actionCat.AddButton("Save and Apply", "Checkmark", "Save and apply the current changes to this tag!");
        var cancel = actionCat.AddButton("Cancel", "TWClose", "Revert changes and exit tag editor");

        editBGColour.OnPress += EditBGColour;
        editTextColour.OnPress += EditTextColour;
        save.OnPress += SaveChanges;
        cancel.OnPress += OnPress;

        
    }

    private void OnPress()
    {
        _currentTagData.TagText = _lastTagText;
        _currentTagData.BackgroundColour = _lastBGColour;
        _currentTagData.TextColour = _lastTextColour;

        

        QuickMenuAPI.GoBack();
    }

    private void SaveChanges()
    {
        if (_modEditor)
        {

        }
        else
        {
            TwTask.Run(TWNetClient.Instance.SendAsync(_currentTagData, TWNetMessageType.TagDataUpdate));
        }

        QuickMenuAPI.GoBack();
    }

    public void OpenTagEditor()
    {
        if (!TWNetClient.Instance.CanUseTag)
        {
            QuickMenuAPI.ShowAlertToast("You either do not have a tag or are not allowed to use it!");
            return;
        }

        _currentTagData = TWNetClient.Instance.CurrentTagData;

        _modEditor = false;
        
        _tagPreviewCategory.CategoryName = $"Tag: {_currentTagData.TagText}";
        _tagEditPage.OpenPage();
    }

    

    private void EditTextColour()
    {
        QuickMenuAPI.OpenColourPicker(_currentTagData.TextColour, (_, s) =>
        {
            _lastTextColour = _currentTagData.TextColour;

            _currentTagData.TextColour = s;

        });
    }

    private void EditBGColour()
    {
        QuickMenuAPI.OpenColourPicker(_currentTagData.BackgroundColour, (_, s) =>
        {
            _lastBGColour = _currentTagData.BackgroundColour;

            _currentTagData.BackgroundColour = s;

        });
    }

    private void EditTagText()
    {
        QuickMenuAPI.OpenKeyboard(_currentTagData.TagText, s =>
        {
            _lastTagText = _currentTagData.TagText;

            _currentTagData.TagText = s;

            CVR_MenuManager.Instance.ToggleQuickMenu(true);
            QuickMenuAPI.ShowAlertToast($"Set tag text to {s}");
            _tagPreviewCategory.CategoryName = $"Tag: {s}";
        });
    }

    
}