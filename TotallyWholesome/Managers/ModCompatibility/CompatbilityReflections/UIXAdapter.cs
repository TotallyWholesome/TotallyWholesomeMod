using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
using WholesomeLoader;

namespace TotallyWholesome.Managers.ModCompatibility.CompatbilityReflections
{
    public static class UIXAdapter
    {
        private static bool? _uixAvailable;
        private static MethodInfo _getExpandedMenu;
        private static MethodInfo _addSimpleButton;
        private static MethodInfo _regSettingEnum;
        private static int _qmEnumValue = -1, _userQMEnumValue = -1, _settingsEnumValue = -1;
        private static bool _methodsGetRan;
        private static bool _oldUIX;
        
        public static bool IsUIXAvailable()
        {
            _uixAvailable ??= MelonMod.RegisteredMelons.Any(x => x.Info.Name.Equals("UI Expansion Kit", StringComparison.OrdinalIgnoreCase));
            return _uixAvailable.Value;
        }

        public static bool GetUIXMethods()
        {
            if (_methodsGetRan) return true;

            Type expandedMenu = Type.GetType("UIExpansionKit.API.ExpansionKitApi, UIExpansionKit");
            Type customLayoutedMenu = Type.GetType("UIExpansionKit.API.ICustomLayoutedMenu, UIExpansionKit");
            Type expandedEnum = Type.GetType("UIExpansionKit.API.ExpandedMenu, UIExpansionKit");

            if (expandedMenu == null || expandedEnum == null || customLayoutedMenu == null) return false;

            _qmEnumValue = (int)_getEnumValue("QuickMenu", expandedEnum);
            _userQMEnumValue = (int)_getEnumValue("UserQuickMenu", expandedEnum);
            _settingsEnumValue = (int)_getEnumValue("SettingsMenu", expandedEnum);
            _getExpandedMenu =  expandedMenu.GetMethod("GetExpandedMenu", BindingFlags.Public | BindingFlags.Static);
            _regSettingEnum = expandedMenu.GetMethod("RegisterSettingAsStringEnum", BindingFlags.Public | BindingFlags.Static);
            _addSimpleButton = customLayoutedMenu.GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(x => x.GetParameters().Length == 2 && x.GetParameters().Any(p => p.ParameterType == typeof(Action)));

            //Attempt to get older UIX AddSimpleButton method
            if (_addSimpleButton == null)
            {
                _addSimpleButton = customLayoutedMenu.GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(x => x.GetParameters().Length == 3 && x.GetParameters().Any(p => p.ParameterType == typeof(Action)));
                _oldUIX = true;
            }

            if (_addSimpleButton == null)
            {
                Con.Error("Unable to reflect AddSimpleButton from UIX, UIXAdapter has failed!");
                return false;
            }

            Con.Debug("Successfully retrieved UIX methods via reflection!");
            
            _methodsGetRan = true;
            return true;
        }

        public static bool AddQMButton(string name, Action func)
        {
            if (!IsUIXAvailable()) return false;
            if (!GetUIXMethods()) return false;
            object menuTarget = _getExpandedMenu.Invoke(null, new object[] { _qmEnumValue });
            
            _addSimpleButton.Invoke(menuTarget, _oldUIX ? new object[] { name, func, null } : new object[] { name, func });

            return true;
        }

        public static bool AddUserQMButton(string name, Action func)
        {
            if (!IsUIXAvailable()) return false;
            if (!GetUIXMethods()) return false;
            
            object menuTarget = _getExpandedMenu.Invoke(null, new object[] { _userQMEnumValue });
            
            _addSimpleButton.Invoke(menuTarget, _oldUIX ? new object[] { name, func, null } : new object[] { name, func });

            return true;
        }
        
        public static bool AddSettingsButton(string name, Action func)
        {
            if (!IsUIXAvailable()) return false;
            if (!GetUIXMethods()) return false;
            
            object menuTarget = _getExpandedMenu.Invoke(null, new object[] { _settingsEnumValue });
            _addSimpleButton.Invoke(menuTarget, _oldUIX ? new object[] { name, func, null } : new object[] { name, func });

            return true;
        }

        public static bool RegSettingsEnum(string settingsCat, string settingsName, IList<(string value, string desc)> values)
        {
            if (!IsUIXAvailable()) return false;
            if (!GetUIXMethods()) return false;

            _regSettingEnum.Invoke(null, new object[] { settingsCat, settingsName, values });

            return true;
        }

        private static object _getEnumValue(string target, Type enumType)
        {
            for (int i = 0; i < enumType.GetEnumNames().Length; i++)
            {
                if (enumType.GetEnumNames()[i].Equals(target))
                    return enumType.GetEnumValues().GetValue(i);
            }

            return -1;
        }
    }
}