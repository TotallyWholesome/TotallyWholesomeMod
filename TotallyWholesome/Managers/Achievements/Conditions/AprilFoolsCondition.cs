using System;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class AprilFoolsCondition : Attribute, ICondition
{
    public bool CheckCondition()
    {
        return LeadManager.Instance != null && LeadManager.Instance.FlippedLeashAccepted;
    }
}