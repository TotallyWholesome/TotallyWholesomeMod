using System;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class HasMasterCondition : Attribute, ICondition
{
    public bool CheckCondition()
    {
        return LeadManager.Instance.MasterPair != null;
    }
}