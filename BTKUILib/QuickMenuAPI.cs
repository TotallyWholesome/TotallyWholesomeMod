using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using BTKUILib.UIObjects.Objects;
using cohtml;

namespace BTKUILib
{
    /// <summary>
    /// This class contains core utilities and some static pages that are used in the QuickMenu
    /// </summary>
    public static class QuickMenuAPI
    {
        /// <summary>
        /// Called when the Cohtml menu is regenerated
        /// </summary>
        public static Action<CVR_MenuManager> OnMenuRegenerate;
        /// <summary>
        /// Called after BTKUILib has finished generating all menu components and BTKUIReady is set
        /// </summary>
        public static Action<CVR_MenuManager> OnMenuGenerated;
        /// <summary>
        /// Called when a user joins the instance, passes the complete CVRPlayerEntity object
        /// </summary>
        public static Action<CVRPlayerEntity> UserLeave;
        /// <summary>
        /// Called when a user leaves the instance, passes the complete CVRPlayerEntity object. Some data may be nulled as the player is leaving
        /// </summary>
        public static Action<CVRPlayerEntity> UserJoin;
        /// <summary>
        /// Fires when a tab change occurs, this includes when the tab is already focused.
        /// First parameter is the target tab, second is the last tab.
        /// </summary>
        public static Action<string, string> OnTabChange;
        /// <summary>
        /// Called when the user is disconnected from a CVR instance
        /// </summary>
        public static Action OnWorldLeave;
        
        /// <summary>
        /// Called when a user is selected in the quick menu, passes the username and user ID
        /// </summary>
        public static Action<string, string> OnPlayerSelected;
        /// <summary>
        /// Called when a page change occurs, passes the new target page and the previous page
        /// </summary>
        public static Action<string, string> OnOpenedPage;
        /// <summary>
        /// Called when back is used, passes the target page and the previous page
        /// </summary>
        public static Action<string, string> OnBackAction;

        /// <summary>
        /// Last selected player's username from the User Select menu
        /// </summary>
        public static string SelectedPlayerName;
        /// <summary>
        /// Last selected player's user ID from the User Select menu
        /// </summary>
        public static string SelectedPlayerID;

        /// <summary>
        /// Player select page for setting up functions that should be used in the context of a user
        /// </summary>
        public static Page PlayerSelectPage { get; internal set; }

        /// <summary>
        /// Contains the currently opened page element ID
        /// </summary>
        public static string CurrentPageID { get; internal set; } = "CVRMainQM";

        /// <summary>
        /// Creates or returns a basic Misc tab page for use by mods not requiring a full tab
        /// </summary>
        public static Page MiscTabPage
        {
            get
            {
                //Create the page as needed
                if (_miscTabPage == null)
                {
                    _miscTabPage = Page.GetOrCreatePage("Misc", "Misc", true, "MiscIcon");
                    _miscTabPage.Protected = true;
                    _miscTabPage.MenuTitle = "Misc";
                    _miscTabPage.MenuSubtitle = "Miscellaneous mod elements be found here!";
                }
                
                return _miscTabPage;
            }
        }

        //Internal actions for utility functions
        internal static Action NoticeOk;
        internal static Action ConfirmYes;
        internal static Action ConfirmNo;
        internal static Action<float> NumberInputComplete;
        internal static Action<string> OnKeyboardSubmitted;
        internal static DateTime TimeSinceKeyboardOpen = DateTime.Now;
        
        private static Page _miscTabPage;

        #region Update Functions

        internal static void UpdateMenuTitle(string title, string subtitle)
        {
            if (!BTKUILib.Instance.IsOnMainThread())
            {
                BTKUILib.Instance.MainThreadQueue.Enqueue(() =>
                {
                    UpdateMenuTitle(title, subtitle);
                });
                return;
            }
            
            if (!UIUtils.IsQMReady()) return;
            
            UIUtils.GetInternalView().TriggerEvent("btkUpdateTitle", title, subtitle);
        }

        #endregion
        
        #region Utility Functions

        /// <summary>
        /// Injects your custom CSS Style into UILib, this will automatically be reapplied during a menu reload
        /// </summary>
        /// <param name="cssData"></param>
        public static void InjectCSSStyle(string cssData)
        {
            UserInterface.CustomCSSStyles.Add(cssData);

            if (!UIUtils.IsQMReady()) return;

            //QM is loaded, let's apply the CSS right now
            UIUtils.GetInternalView().TriggerEvent("btkSetCustomCSS", cssData);
        }

        /// <summary>
        /// Get the MelonLoader prefs tab page for a specific mod, fetched by identifier
        /// </summary>
        /// <param name="prefsIdentifier">Identifier used for the mods MelonPreferences (MelonPreferences_Category.Identifier)</param>
        /// <returns>The created ML prefs page containing the SubpageButton element</returns>
        public static Page GetMLPrefsPageByIdentifier(string prefsIdentifier)
        {
            return !BTKUILib.Instance.MLPrefsPages.ContainsKey(prefsIdentifier) ? null : BTKUILib.Instance.MLPrefsPages[prefsIdentifier];
        }

        /// <summary>
        /// Prepares icons for usage by dropping them in the correct folder
        ///
        /// Icons should be 256x256 in size to avoid issues with CSS, they also need to be PNGs
        /// </summary>
        /// <param name="modName">Your mod name, this should be the same as your pages</param>
        /// <param name="iconName">Name of the icon to be saved</param>
        /// <param name="resourceStream">Stream containing your image data</param>
        public static void PrepareIcon(string modName, string iconName, Stream resourceStream)
        {
            var directory = $"ChilloutVR_Data\\StreamingAssets\\Cohtml\\UIResources\\GameUI\\mods\\BTKUI\\images\\{modName}";

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            var path = $"{directory}\\{iconName}.png";

            using var tempStream = new MemoryStream((int)resourceStream.Length);
            resourceStream.CopyTo(tempStream);
            
            File.WriteAllBytes(path, tempStream.ToArray());
        }

        /// <summary>
        /// Check if an icon was prepared already
        /// </summary>
        /// <param name="modName">Your mod name, this should be the same as your pages</param>
        /// <param name="iconName">Name of the icon you're checking for</param>
        /// <returns></returns>
        public static bool DoesIconExist(string modName, string iconName)
        {
            var directory = $"ChilloutVR_Data\\StreamingAssets\\Cohtml\\UIResources\\GameUI\\mods\\BTKUI\\images\\{modName}";

            return Directory.Exists(directory) && File.Exists($"{directory}\\{iconName}.png");
        }

        /// <summary>
        /// Shows a yes/no confirmation dialog with actions
        /// </summary>
        /// <param name="title">Sets the top title of the dialog window</param>
        /// <param name="content">Sets the main content of the dialog window</param>
        /// <param name="onNo">No/Cancel button action</param>
        /// <param name="onYes">Yes/Confirm button action (Optional)</param>
        /// <param name="yesText">Yes/Confirm button text (Optional, defaults to Yes)</param>
        /// <param name="noText">No/Cancel button text (Optional, defaults to No)</param>
        public static void ShowConfirm(string title, string content, Action onYes, Action onNo = null, string yesText = "Yes", string noText = "No")
        {
            if (!UIUtils.IsQMReady()) return;
            
            ConfirmYes = onYes;
            ConfirmNo = onNo;
            
            UIUtils.GetInternalView().TriggerEvent("btkShowConfirm", title, content, yesText, noText);
        }
        
        /// <summary>
        /// Shows a basic notice dialog with an OK button
        /// </summary>
        /// <param name="title">Sets the top title of the dialog window</param>
        /// <param name="content">Sets the main content of the dialog window</param>
        /// <param name="onOK">Action to be fired upon clicking the OK/Close button</param>
        /// <param name="okText">OK/Close button text</param>
        public static void ShowNotice(string title, string content, Action onOK = null, string okText = "OK")
        {
            if (!UIUtils.IsQMReady()) return;
            
            NoticeOk = onOK;
            UIUtils.GetInternalView().TriggerEvent("btkShowNotice", title, content, okText);
        }
        
        /// <summary>
        /// Opens the number input page, currently limited to 0-9999
        /// </summary>
        /// <param name="name">Sets the text displayed at the top of the page</param>
        /// <param name="input">Initial number input</param>
        /// <param name="onCompleted">Action to be fired when saving the input</param>
        public static void OpenNumberInput(string name, float input, Action<float> onCompleted)
        {
            if (!UIUtils.IsQMReady()) return;
            
            NumberInputComplete = onCompleted;
            UIUtils.GetInternalView().TriggerEvent("btkOpenNumberInput", name, input);
        }
        
        /// <summary>
        /// Opens the multiselection page
        /// </summary>
        /// <param name="multiSelection">Generated and populated MultiSelection object to populate the multiselection page</param>
        public static void OpenMultiSelect(MultiSelection multiSelection)
        {
            if (!UIUtils.IsQMReady()) return;
            
            UserInterface.Instance.SelectedMultiSelect = multiSelection;
            UIUtils.GetInternalView().TriggerEvent("btkOpenMultiSelect", multiSelection.Name, multiSelection.Options, multiSelection.SelectedOption, UserInterface.IsInPlayerList);
        }
        
        /// <summary>
        /// Opens the CVR keyboard
        /// </summary>
        /// <param name="currentValue">Current text in the keyboard</param>
        /// <param name="callback">Action to be called when keyboard text is submitted</param>
        public static void OpenKeyboard(string currentValue, Action<string> callback)
        {
            if (!UIUtils.IsQMReady()) return;
            
            OnKeyboardSubmitted = callback;
            TimeSinceKeyboardOpen = DateTime.Now;
            ViewManager.Instance.openMenuKeyboard(currentValue);
        }

        /// <summary>
        /// Shows an toast alert on the quick menu, stays up for set delay
        /// </summary>
        /// <param name="message">Message to be displayed on the toast</param>
        /// <param name="delay">Delay in seconds before toast is hidden</param>
        public static void ShowAlertToast(string message, int delay = 5)
        {
            if (!UIUtils.IsQMReady()) return;
            
            UIUtils.GetInternalView().TriggerEvent("btkAlertToast", message, delay);
        }

        /// <summary>
        /// Calls the back function, moves back 1 page in the breadcrumbs
        /// </summary>
        public static void GoBack()
        {
            if (!UIUtils.IsQMReady()) return;

            UIUtils.GetInternalView().TriggerEvent("btkBack");
        }

        /// <summary>
        /// Forcefully adds a page to the RootPages list, you should only use this if you are doing weird stuff.
        /// For general usage please use the RootPage parameter on the Page constructor!
        /// </summary>
        /// <param name="page">The page to be added to the RootPages list</param>
        public static void AddRootPage(Page page)
        {
            if (UserInterface.RootPages.Contains(page)) return;
            UserInterface.RootPages.Add(page);
        }
        
        #endregion
    }
}