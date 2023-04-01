using System;
using TotallyWholesome.Managers.AvatarParams;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class RemoteParamCondition : Attribute, ICondition
    {
        private int _enabledParams;

        public bool CheckCondition()
        {
            return AvatarParameterManager.Instance.EnabledParams >= _enabledParams;
        }

        public RemoteParamCondition(int enabledParams)
        {
            _enabledParams = enabledParams;
        }
    }
}