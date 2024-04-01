using System;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class PetTimeCondition : Attribute, ICondition
{
    private int _minutesRequired;
    private string _lastPairKey;
    private DateTime _pairChangeTime;

    public PetTimeCondition(int minutesRequired)
    {
        _minutesRequired = minutesRequired;
    }

    public bool CheckCondition()
    {
        if (LeadManager.Instance.MasterPair == null) return false;

        if (LeadManager.Instance.MasterPair.Key != _lastPairKey)
        {
            //Reset tracking
            _lastPairKey = LeadManager.Instance.MasterPair.Key;
            _pairChangeTime = DateTime.Now;
        }

        return DateTime.Now.Subtract(_pairChangeTime).Minutes >= _minutesRequired;
    }
}