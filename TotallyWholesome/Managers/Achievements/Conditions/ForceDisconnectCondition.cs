using System;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class ForceDisconnectCondition : Attribute, ICondition
    {
        public bool CheckCondition()
        {
            return LeadManager.Instance.ClearLeashWhileLeashed;
        }
    }
}