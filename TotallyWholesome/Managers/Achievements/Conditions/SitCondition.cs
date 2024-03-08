using System;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Shockers;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class SitCondition : Attribute, ICondition
{
    public bool CheckCondition()
    {
        if (LeadManager.Instance.MasterPair == null) return false;
        if (!ConfigManager.Instance.IsActive(AccessType.AllowHeightControl)) return false;
        if (ShockerManager.Instance.HeightControl == null || !ShockerManager.Instance.HeightControl.Enabled)
            return false;
        return true;
    }
}