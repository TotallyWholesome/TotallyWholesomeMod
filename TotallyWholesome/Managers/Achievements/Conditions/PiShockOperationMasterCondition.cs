using System;
using TWNetCommon.Data.ControlPackets;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class PiShockOperationMasterCondition : Attribute, ICondition
    {
        private ShockOperation _operation;
        private int _strength;
        private int _duration;
        private DateTime _lastMasterFired;
        
        public bool CheckCondition()
        {
            if (PiShockManager.Instance.LastOperationFiredMaster == _lastMasterFired) return false;
            if (PiShockManager.Instance.LastOperationMaster != _operation) return false;
            if (PiShockManager.Instance.LastDurationMaster < _duration) return false;
            if (PiShockManager.Instance.LastStrengthMaster < _strength) return false;

            _lastMasterFired = PiShockManager.Instance.LastOperationFiredMaster;

            return true;
        }

        public PiShockOperationMasterCondition(ShockOperation operation, int strength, int duration)
        {
            _operation = operation;
            _strength = strength;
            _duration = duration;
        }
    }
}