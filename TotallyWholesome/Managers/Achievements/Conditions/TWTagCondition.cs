using System;
using TotallyWholesome.Managers.Status;
using TotallyWholesome.Network;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class TWTagCondition : Attribute, ICondition
{
    public bool CheckCondition()
    {
        return TWNetClient.Instance.CanUseTag;
    }
}