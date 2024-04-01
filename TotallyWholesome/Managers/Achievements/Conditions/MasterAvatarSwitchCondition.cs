using System;
using TotallyWholesome.Managers.TWUI.Pages;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class MasterAvatarSwitchCondition : Attribute, ICondition
{
    private int _switchCount;

    public MasterAvatarSwitchCondition(int switchCount = 1)
    {
        _switchCount = switchCount;
    }

    public bool CheckCondition()
    {
        return IndividualPetControl.Instance.MasterAvatarSwitching >= _switchCount;
    }
}