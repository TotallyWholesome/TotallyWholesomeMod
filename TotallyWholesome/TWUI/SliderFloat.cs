using System;
using ABI_RC.Core.InteractionSystem;
using cohtml;

namespace TotallyWholesome.TWUI
{
    public class SliderFloat
    {
        public string SliderID;

        public float SliderValue
        {
            get => _sliderValue;
            set
            {
                _sliderValue = value;
                OnValueUpdated?.Invoke(value);
            }
        }
        
        public Action<float> OnValueUpdated;

        private float _sliderValue;

        public SliderFloat(string sliderID, float initalValue)
        {
            SliderValue = initalValue;
            SliderID = sliderID;
            
            UserInterface.SliderFloats.Add(this);
        }

        public void SetValueUpdateVisual(float value)
        {
            SliderValue = value;
            
            if (!TWUtils.IsQMReady()) return;
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twSliderSetValue", SliderID, value);     
        }

        public void UpdateSlider()
        {
            if (!TWUtils.IsQMReady()) return;
            CVR_MenuManager.Instance.quickMenu.View.TriggerEvent("twSliderSetValue", SliderID, SliderValue);
        }
    }
}