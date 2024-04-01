using System;
using ABI_RC.Systems.Movement;
using TotallyWholesome.Managers.Lead;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class HangingCondition : Attribute, ICondition
{
    private int _secondsRequired;

    public HangingCondition(int secondsRequired)
    {
        _secondsRequired = secondsRequired;
    }

    public bool CheckCondition()
    {
        if (LeadManager.Instance.MasterPair == null || LeadManager.Instance.MasterPair.LineController == null || BetterBetterCharacterController.Instance.IsFlying()) return false;
        return LeadManager.Instance.MasterPair.LineController.IsUngrounded && LeadManager.Instance.MasterPair.LineController.HangingStart.Subtract(DateTime.Now).Seconds >= _secondsRequired;
    }
}