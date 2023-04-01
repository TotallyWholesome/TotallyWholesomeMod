using System;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class ButtplugDeviceCondition : Attribute, ICondition
    {
        private int _deviceCount;
        
        public bool CheckCondition()
        {
            return ButtplugManager.Instance.ButtplugDeviceCount >= _deviceCount;
        }

        public ButtplugDeviceCondition(int deviceCount)
        {
            _deviceCount = deviceCount;
        }
    }
}