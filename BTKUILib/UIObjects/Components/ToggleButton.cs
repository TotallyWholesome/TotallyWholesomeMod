using System;
using ABI_RC.Core.InteractionSystem;
using cohtml;

namespace BTKUILib.UIObjects.Components
{
    /// <summary>
    /// Basic toggle button element
    /// </summary>
    public class ToggleButton : QMInteractable
    {
        /// <summary>
        /// Gets or sets the current state of the toggle, will update on the fly
        /// </summary>
        public bool ToggleValue
        {
            get => _toggleValue;
            set
            {
                _toggleValue = value;
                UpdateToggle();
            }
        }

        /// <summary>
        /// Get or set the current name of the toggle, will update on the fly
        /// </summary>
        public string ToggleName
        {
            get => _toggleName;
            set
            {
                _toggleName = value;
                UpdateToggle();
            }
        }

        /// <summary>
        /// Get or set the current tooltip displayed on this toggle, will update on the fly
        /// </summary>
        public string ToggleTooltip
        {
            get => _toggleTooltip;
            set
            {
                _toggleTooltip = value;
                UpdateToggle();
            }
        }

        /// <summary>
        /// Action to listen for changes of the toggle state
        /// </summary>
        public Action<bool> OnValueUpdated;

        private bool _toggleValue;
        private string _toggleName;
        private string _toggleTooltip;
        private Category _category;

        internal ToggleButton(string toggleText, string toggleTooltip, bool initialValue, Category category)
        {
            _toggleValue = initialValue;
            _toggleName = toggleText;
            _toggleTooltip = toggleTooltip;
            _category = category;

            Parent = category;
            
            ElementID = $"btkUI-Toggle-{UUID}";
        }

        /// <inheritdoc />
        public override void Delete()
        {
            if (Protected)
                BTKUILib.Log.Error($"You cannot delete a protected element! ElementID: {ElementID}");
            
            _category.SubElements.Remove(this);
            
            UserInterface.QMElements.Remove(this);

            if (!UIUtils.IsQMReady()) return;
            UIUtils.GetInternalView().TriggerEvent("btkDeleteElement", $"{ElementID}-Root");
        }

        internal override void OnInteraction(bool? toggle = null)
        {
            if (toggle == null)
            {
                BTKUILib.Log.Error("Toggle received an event that contained a null toggle state! That shouldn't happen!");
                return;
            }

            _toggleValue = toggle.Value;
            OnValueUpdated?.Invoke(_toggleValue);
        }

        internal override void GenerateCohtml()
        {
            if (!UIUtils.IsQMReady()) return;

            if (RootPage is { IsVisible: false }) return;

            if(!IsGenerated)
                UIUtils.GetInternalView().TriggerEvent("btkCreateToggle", _category.ElementID, _toggleName, UUID, _toggleTooltip, _toggleValue);
            
            base.GenerateCohtml();

            IsGenerated = true;
        }

        private void UpdateToggle()
        {
            if(!IsVisible) return;

            if (!BTKUILib.Instance.IsOnMainThread())
            {
                BTKUILib.Instance.MainThreadQueue.Enqueue(UpdateToggle);
                return;
            }

            if (!UIUtils.IsQMReady()) return;
            
            UIUtils.GetInternalView().TriggerEvent("btkSetToggleState", ElementID, _toggleValue);
            UIUtils.GetInternalView().TriggerEvent("btkUpdateTooltip", $"{ElementID}-Tooltip", _toggleTooltip);
            UIUtils.GetInternalView().TriggerEvent("btkUpdateText", $"{ElementID}-Text", _toggleName);
        }
    }
}