using System;
using TWNetCommon.Data.ControlPackets;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class PiShockLimitCondition : Attribute, ICondition
    {
        private int _strength;
        private int _duration;

        public bool CheckCondition()
        {
            if (PiShockManager.Instance.LastShockerInfo == null) return false;
            
            lock (PiShockManager.Instance.LastShockerInfo)
            {
                var info = PiShockManager.Instance.LastShockerInfo;

                if (info.MaxDuration < _duration) return false;
                if (info.MaxIntensity < _strength) return false;
            }

            return true;
        }

        public PiShockLimitCondition(int strength, int duration)
        {
            _strength = strength;
            _duration = duration;
        }
    }
}