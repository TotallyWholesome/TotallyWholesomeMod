using System;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class TugOfWarPullCondition : Attribute, ICondition
    {
        private int _pullDuration;
        private DateTime _pullStart;
        private bool _pulling;

        public bool CheckCondition()
        {
            if (LeadManager.Instance.MasterPair == null || LeadManager.Instance.MasterPair.LineController == null) return false;
            if (LeadManager.Instance.TugOfWarPair == null || LeadManager.Instance.TugOfWarPair.LineController == null) return false;

            if (LeadManager.Instance.MasterPair.LineController.IsAtMaxLeashLimit && LeadManager.Instance.TugOfWarPair.LineController.IsAtMaxLeashLimit)
            {
                if (!_pulling)
                {
                    _pullStart = DateTime.Now;
                    _pulling = true;
                }

                return DateTime.Now.Subtract(_pullStart).TotalSeconds >= _pullDuration;
            }

            _pulling = false;
            return false;
        }
        
        public TugOfWarPullCondition(int pullDuration)
        {
            _pullDuration = pullDuration;
        }
    }
}