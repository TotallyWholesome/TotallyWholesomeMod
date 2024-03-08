using System;
using TotallyWholesome.Managers.Shockers;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class ShockerNoLimitsLimitCondition : Attribute, ICondition
{
    public bool CheckCondition()
    {
        return ShockerManager.Instance.ShockerProvider?.NoLimits ?? false;
    }
}