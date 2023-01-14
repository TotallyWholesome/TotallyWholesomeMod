using System;
using ABI.CCK.Scripts;

namespace TotallyWholesome.Managers.AvatarParams
{
    public class AvatarParameter
    {
        public string Name;
        public bool RemoteEnabled;
        public CVRAdvancedSettingsEntry.SettingsType ParamType;
        public string GeneratedType;
        public string[] Options = Array.Empty<string>();
        public float CurrentValue; //We only care about the X value, no support for remote vector types

        public override string ToString()
        {
            string optionsString = "empty";

            if (Options != null)
                optionsString = string.Join(", ", Options);

            return $"AvatarParameter - [Name: {Name}, RemoteEnable: {RemoteEnabled}, ParamType: {ParamType}, GeneratedType: {GeneratedType}, Options: [{optionsString}], CurrentValue: {CurrentValue}]";
        }
    }
}