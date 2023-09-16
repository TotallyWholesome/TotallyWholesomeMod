using System;
using TotallyWholesome.Network;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class LoginCondition : Attribute, ICondition
    {
        public bool CheckCondition()
        {
            return TWNetClient.Instance.IsTWNetConnected();
        }
    }
}