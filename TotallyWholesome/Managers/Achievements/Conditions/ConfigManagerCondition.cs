using System;

namespace TotallyWholesome.Managers.Achievements.Conditions
{
    public class ConfigManagerCondition : Attribute, ICondition
    {
        private AccessType[] _enabledConfigs;
        
        public bool CheckCondition()
        {
            foreach (var accessType in _enabledConfigs)
            {
                if (!ConfigManager.Instance.IsActive(accessType))
                    return false;
            }

            return true;
        }

        public ConfigManagerCondition(params AccessType[] enabledConfigs)
        {
            _enabledConfigs = enabledConfigs;
        }
    }
}