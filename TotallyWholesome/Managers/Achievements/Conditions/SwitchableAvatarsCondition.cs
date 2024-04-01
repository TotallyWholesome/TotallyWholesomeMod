using System;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class SwitchableAvatarsCondition : Attribute, ICondition
{
    private int _targetCount;

    public SwitchableAvatarsCondition(int targetCount = 1)
    {
        _targetCount = targetCount;
    }

    public bool CheckCondition()
    {
        return Configuration.JSONConfig.SwitchingAllowedAvatars.Count >= _targetCount;
    }
}