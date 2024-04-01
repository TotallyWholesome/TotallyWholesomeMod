using System;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class MasterBypassCondition : Attribute, ICondition
{
    public bool CheckCondition()
    {
        return PlayerRestrictionManager.Instance.MasterBypassApplied;
    }
}