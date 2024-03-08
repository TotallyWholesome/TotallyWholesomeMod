using System;
using System.Reflection;
using System.Text.RegularExpressions;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.IO.Social;
using TotallyWholesome.Managers;
using TotallyWholesome.TWUI.UIObjects.Objects;

namespace TotallyWholesome.TWUI
{
    public class UIUtils
    {
        public static Action NoticeOk;
        public static Action ConfirmYes;
        public static Action ConfirmNo;
        public static Action<float> NumberInputComplete;
        
        private static FieldInfo _qmReady = typeof(CVR_MenuManager).GetField("_quickMenuReady", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void SendModInit()
        {
            TWUtils.GetInternalView().TriggerEvent("twModInit", ConfigManager.Instance.IsActive(AccessType.HidePiShock), ConfigManager.Instance.IsActive(AccessType.HideToyIntegration), Configuration.JSONConfig.LogoPositionX, Configuration.JSONConfig.LogoPositionY);
        }

        public static void ShowToast(string message, int delay = 5)
        {
            if (!TWUtils.IsQMReady()) return;
            TWUtils.GetInternalView().TriggerEvent("twAlertToast", message, delay);
        }

        public static void ShowNotice(string title, string content, string okText = "OK", Action onOK = null)
        {
            NoticeOk = onOK;
            TWUtils.GetInternalView().TriggerEvent("twShowNotice", title, content, okText);
        }

        public static void OpenMultiSelect(MultiSelection multiSelection)
        {
            UserInterface.Instance.SelectedMultiSelect = multiSelection;
            TWUtils.GetInternalView().TriggerEvent("twOpenMultiSelect", multiSelection.Name, multiSelection.Options, multiSelection.SelectedOption);
        }

        public static void ShowConfirm(string title, string content, string yesText = "Yes", Action onYes = null, string noText = "No", Action onNo = null)
        {
            ConfirmYes = onYes;
            ConfirmNo = onNo;
            
            TWUtils.GetInternalView().TriggerEvent("twShowConfirm", title, content, yesText, noText);
        }

        public static void SetToggleState(string toggleID, bool state, string category = null, string pageID = null)
        {
            string target = "twUI-Toggle-";
            if (category != null)
                target += $"{category}-";
            if (pageID != null)
                target += $"{pageID}-";
            target += $"{toggleID}";
            
            TWUtils.GetInternalView().TriggerEvent("twSetToggleState", target, state);
        }

        public static string CreateToggle(string pageID, string settingsCategory, string toggleName, string toggleID, string tooltip, bool state)
        {
            TWUtils.GetInternalView().TriggerEvent("twCreateToggle", settingsCategory, pageID, toggleName, toggleID, tooltip, state);
            return $"{settingsCategory}-{pageID}-{toggleID}";
        }

        public static SliderFloat CreateSlider(string parent, string sliderName, string sliderID, float currentValue, float minValue, float maxValue, string tooltip)
        {
            TWUtils.GetInternalView().TriggerEvent("twCreateSlider", parent, sliderName, $"{sliderID}", currentValue, minValue, maxValue, tooltip);
            return new SliderFloat($"{sliderID}", currentValue);
        }

        public static void CreateButton(string parent, string buttonName, string buttonIcon, string tooltip, string buttonAction)
        {
            TWUtils.GetInternalView().TriggerEvent("twCreateButton", parent, buttonName, buttonIcon, tooltip, buttonAction);
        }

        public static void OpenNumberInput(string name, float input, Action<float> onCompleted)
        {
            NumberInputComplete = onCompleted;
            TWUtils.GetInternalView().TriggerEvent("twOpenNumberInput", name, input);
        }
        
        /// <summary>
        /// Check if the CVR_MenuManager view is ready
        /// </summary>
        /// <returns>True if view is ready, false if it's not</returns>
        public static bool IsQMReady()
        {
            if (CVR_MenuManager.Instance == null)
                return false;

            return (bool)_qmReady.GetValue(CVR_MenuManager.Instance) && UserInterface.TWUIReady;
        }

        /// <summary>
        /// Clean non alphanumeric characters from a given string
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Cleaned string</returns>
        public static string GetCleanString(string input)
        {
            return Regex.Replace(Regex.Replace(input, "<.*?>", string.Empty), @"[^0-9a-zA-Z_]+", string.Empty);
        }
    }
}