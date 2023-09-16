using System;
using TotallyWholesome.Network;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class RatelimitedCondition : Attribute, ICondition
    {
        public bool CheckCondition()
        {
            return TWNetClient.Instance.HasBeenRatelimited;
        }
    }
}