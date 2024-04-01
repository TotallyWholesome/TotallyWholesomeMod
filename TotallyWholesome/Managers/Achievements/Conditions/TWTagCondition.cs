using System;
using TotallyWholesome.Managers.Status;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class TWTagCondition : Attribute, ICondition
{
    public bool CheckCondition()
    {
        return StatusManager.Instance.IsTagCustom();
    }
}