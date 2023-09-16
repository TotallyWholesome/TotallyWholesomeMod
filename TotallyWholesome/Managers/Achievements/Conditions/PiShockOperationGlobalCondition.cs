using System;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class PiShockOperationGlobalCondition : Attribute, ICondition
    {
        private DateTime _lastMasterFired;

        public bool CheckCondition()
        {
            if (PiShockManager.Instance.LastOperationGlobalMaster == _lastMasterFired) return false;

            _lastMasterFired = PiShockManager.Instance.LastOperationGlobalMaster;

            return true;
        }
    }
}