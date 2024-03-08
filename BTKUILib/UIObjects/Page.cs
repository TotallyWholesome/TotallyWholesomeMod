using System;
using System.Collections.Generic;
using System.Linq;
using ABI_RC.Core.InteractionSystem;
using BTKUILib.UIObjects.Components;
// ReSharper disable MethodOverloadWithOptionalParameter

namespace BTKUILib.UIObjects
{
    /// <summary>
    /// This object represents a page that exists in Cohtml
    /// </summary>
    public class Page : QMUIElement
    {
        /// <summary>
        /// Get or set the menu title displayed at the very top of the QM, will update on the fly
        /// </summary>
        public string MenuTitle
        {
            get => _menuTitle;
            set
            {
                _menuTitle = value;
                if(!Protected && IsVisible)
                    QuickMenuAPI.UpdateMenuTitle(_menuTitle, _menuSubtitle);
            }
        }

        /// <summary>
        /// Get or set the menu subtitle displayed at the very top of the QM, will update on the fly
        /// </summary>
        public string MenuSubtitle
        {
            get => _menuSubtitle;
            set
            {
                _menuSubtitle = value;
                if(!Protected && IsVisible)
                    QuickMenuAPI.UpdateMenuTitle(_menuTitle, _menuSubtitle);
            }
        }

        /// <summary>
        /// Get or set the display name for a page, this will only work on non root pages and will update the header with whatever you set
        /// </summary>
        public string PageDisplayName
        {
            get => _displayName;
            set
            {
                if (IsRootPage)
                {
                    BTKUILib.Log.Warning("Setting the DisplayName on a Root Page will do nothing!");
                    return;
                }
                _displayName = value;

                if (!UIUtils.IsQMReady() || !IsGenerated || !IsVisible) return;

                UIUtils.GetInternalView().TriggerEvent("btkUpdatePageTitle", ElementID, value);
            }
        }

        /// <summary>
        /// Sets if this pages tab is visible or not
        /// </summary>
        public bool HideTab
        {
            get => _hideTab;
            set
            {
                if (!IsRootPage || _noTab)
                {
                    BTKUILib.Log.Warning($"Page \"{PageName}\" of mod \"{ModName}\" does not have an associated tab! Cannot update tab state!");
                    return;
                }

                _hideTab = value;

                if (!UIUtils.IsQMReady() || !TabGenerated) return;

                UIUtils.GetInternalView().TriggerEvent("btkUpdateTab", ModName, value);
            }
        }

        /// <inheritdoc />
        public override bool Hidden
        {
            get => base.Hidden;
            set
            {
                if (SubpageButton != null)
                    SubpageButton.Hidden = value;

                base.Hidden = value;
            }
        }

        /// <inheritdoc />
        public override bool Disabled
        {
            get => base.Disabled;
            set
            {
                if (SubpageButton != null)
                    SubpageButton.Disabled = value;

                base.Disabled = value;
            }
        }

        /// <summary>
        /// Reference to the button that opens this subpage
        /// </summary>
        public Button SubpageButton
        {
            get => _subpageButton;
            internal set => _subpageButton = value;
        }

        internal bool IsRootPage;
        internal string PageName = "MainPage";
        internal readonly string ModName;
        internal bool InPlayerlist = false;
        internal bool TabGenerated = false;
        private Button _subpageButton;

        private string _displayName;
        private string _menuSubtitle;
        private string _tabIcon;
        private string _menuTitle;
        private Category _category;
        private string _tabID;
        private bool _noTab;
        private bool _hideTab;

        /// <summary>
        /// Create a new page object, this will automatically be created within Cohtml when it is ready
        /// </summary>
        /// <param name="modName">Name of your mod, you can use this to have multiple mods use the same root tab</param>
        /// <param name="pageName">Name of the page, this isn't visible anywhere</param>
        /// <param name="isRootPage">Sets if this page should also generate a tab</param>
        /// <param name="tabIcon">Icon to be displayed on the tab</param>
        /// <param name="category">Only set if this page was created from a category</param>
        public Page(string modName, string pageName, bool isRootPage = false, string tabIcon = null, Category category = null) : this(modName, pageName, isRootPage, tabIcon, category, false){}

        /// <summary>
        /// Create a new page object, this will automatically be created within Cohtml when it is ready
        /// </summary>
        /// <param name="modName">Name of your mod, you can use this to have multiple mods use the same root tab</param>
        /// <param name="pageName">Name of the page, this isn't visible anywhere</param>
        /// <param name="isRootPage">Sets if this page should also generate a tab</param>
        /// <param name="tabIcon">Icon to be displayed on the tab</param>
        /// <param name="category">Only set if this page was created from a category</param>
        /// <param name="noTab">Sets if this page should not generate a tab, only functions for rootpages</param>
        public Page(string modName, string pageName, bool isRootPage, string tabIcon, Category category, bool noTab)
        {
            PageName = pageName;
            _displayName = pageName;

            ModName = modName;
            IsRootPage = isRootPage;
            _tabIcon = tabIcon;
            _category = category;
            _noTab = noTab;

            Parent = category;

            ElementID = $"btkUI-{UIUtils.GetCleanString(modName)}-{UIUtils.GetCleanString(pageName)}";

            if (isRootPage)
            {
                UserInterface.Instance.RegisterRootPage(this);
                _tabID = $"btkUI-Tab-{UIUtils.GetCleanString(modName)}";
            }

            if (UserInterface.Instance.AddModPage(modName, this))
            {
                BTKUILib.Log.Warning($"The page \"{pageName}\" of mod \"{modName}\" appears to have already been created! Tell the creator of this to switch to Page.GetOrCreatePage to ensure they use the existing page properly!");
            }
        }

        /// <summary>
        /// Internal use only, maps this page element to an existing element in the menu
        /// </summary>
        /// <param name="elementID">ElementID matching the existing element</param>
        internal Page(string elementID)
        {
            Protected = true;
            ModName = "BTKUILib";
            UserInterface.RootPages.Add(this);
            ElementID = elementID;
        }

        /// <summary>
        /// Attempts to get an existing page matching the modName and pageName given, otherwise creates a new page
        /// </summary>
        /// <param name="modName">Name of your mod, you can use this to have multiple mods use the same root tab</param>
        /// <param name="pageName">Name of the page, this isn't visible anywhere</param>
        /// <param name="isRootPage">Sets if this page should also generate a tab</param>
        /// <param name="tabIcon">Icon to be displayed on the tab</param>
        /// <param name="category">Only set if this page was created from a category</param>
        /// <param name="noTab">Sets if this page should not generate a tab, only functions for rootpages</param>
        /// <returns>New or existing page object</returns>
        public static Page GetOrCreatePage(string modName, string pageName, bool isRootPage = false, string tabIcon = null, Category category = null, bool noTab = false)
        {
            if (UserInterface.ModPages.TryGetValue(modName, out var pages))
            {
                var page = pages.FirstOrDefault(x => x.PageName == pageName);

                if (page != null)
                    return page;
            }

            return new Page(modName, pageName, isRootPage, tabIcon, category, noTab);
        }

        /// <summary>
        /// Opens this page in cohtml
        /// </summary>
        public void OpenPage()
        {
            OpenPage(false);
        }

        /// <summary>
        /// Opens this page in Cohtml with optional resetBreadcrumbs param
        /// <param name="resetBreadcrumbs">Set this true to reset the breadcrumbs back to the root page</param>
        /// </summary>
        public void OpenPage(bool resetBreadcrumbs)
        {
            if (!UIUtils.IsQMReady()) return;

            if (!RootPage.IsVisible && (RootPage != this || IsRootPage))
            {
                //We need to trigger a tab change first!
                UserInterface.Instance.OnTabChange(ElementID);
            }

            if (!IsVisible && RootPage == this && !IsRootPage)
            {
                //This is a standalone "subpage" rootpage, don't reset the breadcrumbs!
                IsVisible = true;
                GenerateCohtml();
            }

            if (resetBreadcrumbs)
            {
                UIUtils.GetInternalView().TriggerEvent("btkPushPage", ElementID, true);
                return;
            }
            
            UIUtils.GetInternalView().TriggerEvent("btkPushPage", ElementID);
        }

        /// <summary>
        /// Add a new category (row) to this page
        /// </summary>
        /// <param name="categoryName">Name of the category, displayed at the top</param>
        /// <returns>A newly created category</returns>
        public Category AddCategory(string categoryName)
        {
            return AddCategory(categoryName, true);
        }

        /// <summary>
        /// Add a new category (row) to this page
        /// </summary>
        /// <param name="categoryName">Name of the category, displayed at the top</param>
        /// <param name="showHeader">Sets if the header of this category is visible</param>
        /// <returns></returns>
        public Category AddCategory(string categoryName, bool showHeader)
        {
            return AddCategory(categoryName, showHeader, true, false);
        }

        /// <summary>
        /// Add a new category (row) to this page
        /// </summary>
        /// <param name="categoryName">Name of the category, displayed at the top</param>
        /// <param name="showHeader">Sets if the header of this category is visible</param>
        /// <param name="canCollapse">Sets if this category can be collapsed</param>
        /// <param name="collapsed">Sets if this category should be created as collapsed</param>
        /// <returns></returns>
        public Category AddCategory(string categoryName, bool showHeader, bool canCollapse = true, bool collapsed = false)
        {
            return AddCategory(categoryName, null, showHeader, canCollapse, collapsed);
        }

        /// <summary>
        /// Add a new category to this page, modName should be null unless this is a protected page (PlayerSelectPage or Misc page)
        /// </summary>
        /// <param name="categoryName">Name of the category, displayed at the top</param>
        /// <param name="modName">Name of the mod creating the category, this must match your prepared icon modname to use icons</param>
        /// <param name="showHeader">Sets if the header of this category is visible</param>
        /// <param name="canCollapse">Sets if this category can be collapsed</param>
        /// <param name="collapsed">Sets if this category should be created as collapsed</param>
        /// <returns></returns>
        public Category AddCategory(string categoryName, string modName, bool showHeader, bool canCollapse, bool collapsed)
        {
            if(!Protected && modName != null)
                BTKUILib.Log.Warning("You should not be using AddCategory(categoryName, modName, showHeader, canCollapse, collapsed) on your created pages! This is only intended for special protected pages! (PlayerSelectPage and Misc page)");

            var category = new Category(categoryName, this, showHeader, modName, canCollapse, collapsed);
            SubElements.Add(category);

            if (UIUtils.IsQMReady())
                category.GenerateCohtml();

            return category;
        }
        
        /// <summary>
        /// Add a new category to this page, for use on Protected pages only
        /// </summary>
        /// <param name="categoryName">Name of the category, displayed at the top</param>
        /// <param name="modName">Name of the mod creating the category, should match other usages</param>
        /// <returns>A newly created category</returns>
        public Category AddCategory(string categoryName, string modName)
        {
            if(!Protected)
                BTKUILib.Log.Warning("You should not be using AddCategory(categoryName, modName) on your created pages! This is only intended for special protected pages! (PlayerSelectPage and Misc page)");
            
            var category = new Category(categoryName, this, true, modName);
            SubElements.Add(category);

            if (UIUtils.IsQMReady()) 
                category.GenerateCohtml();

            return category;
        }

        /// <summary>
        /// Create a slider on the page
        /// </summary>
        /// <param name="sliderName">Name of the slider, displayed above the slider</param>
        /// <param name="sliderTooltip">Tooltip displayed when hovering on the slider</param>
        /// <param name="initialValue">Initial value of the slider</param>
        /// <param name="minValue">Minimum value that the slider can slide to</param>
        /// <param name="maxValue">Maximum value the slider can slide to</param>
        /// <returns></returns>
        [Obsolete("You should move to using Category.AddSlider instead of Page.AddSlider! This function may be removed in future versions of UILib!")]
        public SliderFloat AddSlider(string sliderName, string sliderTooltip, float initialValue, float minValue, float maxValue)
        {
            return AddSlider(sliderName, sliderTooltip, initialValue, minValue, maxValue, 2, 0f, false);
        }

        /// <summary>
        /// Create a slider on the page
        /// </summary>
        /// <param name="sliderName">Name of the slider, displayed above the slider</param>
        /// <param name="sliderTooltip">Tooltip displayed when hovering on the slider</param>
        /// <param name="initialValue">Initial value of the slider</param>
        /// <param name="minValue">Minimum value that the slider can slide to</param>
        /// <param name="maxValue">Maximum value the slider can slide to</param>
        /// <param name="decimalPlaces">Set the number of decimal places displayed on the slider</param>
        /// <returns></returns>
        [Obsolete("You should move to using Category.AddSlider instead of Page.AddSlider! This function may be removed in future versions of UILib!")]
        public SliderFloat AddSlider(string sliderName, string sliderTooltip, float initialValue, float minValue, float maxValue, int decimalPlaces)
        {
            return AddSlider(sliderName, sliderTooltip, initialValue, minValue, maxValue, decimalPlaces, 0f, false);
        }

        /// <summary>
        /// Create a slider on the page
        /// </summary>
        /// <param name="sliderName">Name of the slider, displayed above the slider</param>
        /// <param name="sliderTooltip">Tooltip displayed when hovering on the slider</param>
        /// <param name="initialValue">Initial value of the slider</param>
        /// <param name="minValue">Minimum value that the slider can slide to</param>
        /// <param name="maxValue">Maximum value the slider can slide to</param>
        /// <param name="decimalPlaces">Set the number of decimal places displayed on the slider</param>
        /// <param name="defaultValue">Default value for this slider</param>
        /// <param name="allowReset">Allow this slider to be reset using the reset button</param>
        /// <returns></returns>
        [Obsolete("You should move to using Category.AddSlider instead of Page.AddSlider! This function may be removed in future versions of UILib!")]
        public SliderFloat AddSlider(string sliderName, string sliderTooltip, float initialValue, float minValue, float maxValue, int decimalPlaces, float defaultValue, bool allowReset)
        {
            var slider = new SliderFloat(this, sliderName, sliderTooltip, initialValue, minValue, maxValue, decimalPlaces, defaultValue, allowReset);
            SubElements.Add(slider);
            
            if(UIUtils.IsQMReady())
                slider.GenerateCohtml();

            return slider;
        }

        /// <inheritdoc />
        public override void Delete()
        {
            base.Delete();

            if (Protected) return;
            
            if (IsRootPage)
            {
                UserInterface.RootPages.Remove(this);
                UIUtils.GetInternalView().TriggerEvent("btkDeleteElement", _tabID);
            }

            //Remove this page from the category list
            if(_category != null && _category.SubElements.Contains(this))
                _category.SubElements.Remove(this);
        }
        
        /// <summary>
        /// Deletes all children of this page
        /// </summary>
        public void ClearChildren()
        {
            //Iterate through each subelement and ensure ClearChildren and Delete is fired
            foreach (var subElement in SubElements.ToArray())
            {
                if(subElement.Deleted) continue;

                switch (subElement)
                {
                    case Page page:
                        page.ClearChildren();
                        break;
                    case Category cat:
                        cat.ClearChildren();
                        break;
                }

                subElement.Delete();
            }

            SubElements.Clear();

            if(!IsVisible) return;
            UIUtils.GetInternalView().TriggerEvent("btkClearChildren", ElementID + "-Content");
        }

        internal void GenerateTab()
        {
            if (!UIUtils.IsQMReady() || TabGenerated || _noTab || !IsRootPage) return;

            UIUtils.GetInternalView().TriggerEvent("btkCreateTab", _displayName, ModName, _tabIcon, UIUtils.GetCleanString(PageName));

            TabGenerated = true;

            UIUtils.GetInternalView().TriggerEvent("btkUpdateTab", ModName, HideTab);
        }

        internal override void DeleteInternal(bool tabChange = false)
        {
            base.DeleteInternal(tabChange);

            if(tabChange) return;

            if (IsRootPage)
            {
                UserInterface.RootPages.Remove(this);
                UIUtils.GetInternalView().TriggerEvent("btkDeleteElement", _tabID);
            }

            //Remove this page from the category list
            if(_category != null && _category.SubElements.Contains(this))
                _category.SubElements.Remove(this);

            SubElements.Clear();
        }
        
        internal override void GenerateCohtml()
        {
            if (!UIUtils.IsQMReady()) return;

            if (RootPage is { IsVisible: false }) return;

            if (!IsGenerated)
            {
                UIUtils.GetInternalView().TriggerEvent("btkCreatePage", _displayName, ModName, _tabIcon, ElementID, IsRootPage, UIUtils.GetCleanString(PageName), InPlayerlist, _noTab);

                if(!Protected)
                    UserInterface.GeneratedPages.Add(this);
            }

            IsGenerated = true;
            
            foreach (var category in SubElements)
            {
                category.GenerateCohtml();
            }
        }

        internal void TabChange()
        {
            IsGenerated = false;

            //Recursively delete sub elements that need special handling
            foreach (var element in SubElements)
            {
                element.IsGenerated = false;

                switch (element)
                {
                    case Category:
                    case Page:
                        element.DeleteInternal(true);
                        break;
                }
            }

            if (!UIUtils.IsQMReady()) return;
            UIUtils.GetInternalView().TriggerEvent("btkDeleteElement", ElementID);
        }
    }
}