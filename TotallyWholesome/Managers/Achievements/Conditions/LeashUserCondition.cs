using System;
using System.Linq;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class LeashUserCondition : Attribute, ICondition
    {
        private bool _checkMaster;
        private string[] _targetIDs;

        public LeashUserCondition(bool checkMaster, params string[] targetIDs)
        {
            _checkMaster = checkMaster;
            _targetIDs = targetIDs;
        }

        public bool CheckCondition()
        {
            return _checkMaster ? _targetIDs.Contains(LeadManager.Instance.MasterPair?.MasterID) : LeadManager.Instance.PetPairs.Any(x => _targetIDs.Contains(x.PetID));
        }
    }
}