using System;
using TotallyWholesome.Network;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class DisconnectCondition : Attribute, ICondition
    {
        public bool CheckCondition()
        {
            return !TWNetClient.Instance.IsTWNetConnected() && TWNetClient.Instance.ExpectedDisconnect;
        }
    }
}