using System;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class RecreatedLeashCondition : Attribute, ICondition
    {
        private int _creationsRequired;
        private DateTime _lastCreation;
        private int _createdCount;
        
        public bool CheckCondition()
        {
            if (_lastCreation != LeadManager.Instance.RecreatedLeashFromLastInstance)
            {
                _lastCreation = LeadManager.Instance.RecreatedLeashFromLastInstance;
                _createdCount++;
            }

            return _createdCount >= _creationsRequired;
        }

        public RecreatedLeashCondition(int creationsRequired)
        {
            _creationsRequired = creationsRequired;
        }
    }
}