using System;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class SitCondition : Attribute, ICondition
    {
        public bool CheckCondition()
        {
            if (LeadManager.Instance.MasterPair == null) return false;
            if (!ConfigManager.Instance.IsActive(AccessType.AllowHeightControl)) return false;
            if (PiShockManager.Instance.lastPacket == null || !PiShockManager.Instance.lastPacket.ShockHeightEnabled) return false;
            return true;
        }

        
    }
}