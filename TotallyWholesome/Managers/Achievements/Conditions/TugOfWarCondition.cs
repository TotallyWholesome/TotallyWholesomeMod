using System;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class TugOfWarCondition : Attribute, ICondition
    {
        public bool CheckCondition()
        {
            if (LeadManager.Instance.MasterPair == null || LeadManager.Instance.MasterPair.LineController == null) return false;
            if (LeadManager.Instance.TugOfWarPair == null || LeadManager.Instance.TugOfWarPair.LineController == null) return false;
            return true;
        }
    }
}