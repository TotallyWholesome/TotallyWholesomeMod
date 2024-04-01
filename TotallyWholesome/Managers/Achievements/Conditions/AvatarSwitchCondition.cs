using System;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class AvatarSwitchCondition : Attribute, ICondition
{
    private int _switchCount;

    public AvatarSwitchCondition(int switchCount = 1)
    {
        _switchCount = switchCount;
    }

    public bool CheckCondition()
    {
        return PlayerRestrictionManager.Instance.AvatarSwitched >= _switchCount;
    }
}