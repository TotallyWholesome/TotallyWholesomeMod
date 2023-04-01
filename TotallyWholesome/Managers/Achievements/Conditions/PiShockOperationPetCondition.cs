using System;
using TWNetCommon.Data.ControlPackets;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class PiShockOperationPetCondition: Attribute, ICondition
    {
        private ShockOperation _operation;
        private int _strength;
        private int _duration;
        private DateTime _lastPetFired;

        public bool CheckCondition()
        {
            if (PiShockManager.Instance.LastOperationFiredPet == _lastPetFired) return false;
            if (PiShockManager.Instance.LastOperationPet != _operation) return false;
            if (PiShockManager.Instance.LastDurationPet < _duration) return false;
            if (PiShockManager.Instance.LastStrengthPet < _strength) return false;

            _lastPetFired = PiShockManager.Instance.LastOperationFiredPet;

            return true;
        }

        public PiShockOperationPetCondition(ShockOperation operation, int strength, int duration)
        {
            _operation = operation;
            _strength = strength;
            _duration = duration;
        }
    }
}