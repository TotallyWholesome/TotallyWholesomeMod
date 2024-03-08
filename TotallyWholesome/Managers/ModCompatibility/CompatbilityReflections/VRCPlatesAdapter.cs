using System;
using System.Linq;
using System.Reflection;
using ABI_RC.Core.Player;
using HarmonyLib;
using MelonLoader;
using TotallyWholesome.Managers.Status;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome.Managers.ModCompatibility.CompatbilityReflections
{
    public class VRCPlatesAdapter
    {
        private static MelonPreferences_Entry<bool> _vrcPlatesEnabled;
        private static bool? _vrcPlatesAvailable;
        private static bool _methodFail;
        internal static MethodInfo _addNameplateMethod;

        public static bool IsVRCPlatesEnabled()
        {
            _vrcPlatesAvailable ??= MelonMod.RegisteredMelons.Any(x => x.Info.Name.Equals("VRCPlates", StringComparison.OrdinalIgnoreCase));
            
            if(_vrcPlatesEnabled == null)
                return _vrcPlatesAvailable.Value && !_methodFail;
            
            return _vrcPlatesAvailable.Value && !_methodFail && _vrcPlatesEnabled.Value;
        }

        public static void SetupVRCPlateCompat()
        {
            if (!IsVRCPlatesEnabled()) return;
            
            _vrcPlatesEnabled = MelonPreferences.GetEntry<bool>("ClassicNameplates", "_enabled");
            
            var npMan = Type.GetType("VRCPlates.NameplateManager, VRCPlates");
            if (npMan == null)
            {
                Con.Warn("NameplateManager class was not found in VRCPlates! VRCPlates compatibility will not work.");
                _methodFail = true;
                return;
            }
            
            _addNameplateMethod = npMan.GetMethod("AddNameplate", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_addNameplateMethod == null)
            {
                Con.Warn("AddNameplate method was not found in VRCPlates! VRCPlates compatibility will not work.");
                _methodFail = true;
                return;
            }

            var type = typeof(VRCPlatesAddNameplateHook);

            try {
                HarmonyLib.Harmony.CreateAndPatchAll(type, BuildInfo.Name + "_Hooks");
            } catch (Exception e) {
                Con.Error("VRCPlates compatibility will not be functional, a problem occured with the AddNameplate patch!");
                _methodFail = true;
                Con.Error($"Failed while patching {type.Name}!\n{e}");
            }
        }
    }

    [HarmonyPatch]
    class VRCPlatesAddNameplateHook
    {
        static MethodInfo TargetMethod()
        {
            return VRCPlatesAdapter._addNameplateMethod;
        }

        static void Postfix(MonoBehaviour nameplate, CVRPlayerEntity player)
        {
            try
            {
                StatusManager.Instance.OnNameplateRebuild(player.PlayerDescriptor, nameplate.gameObject.transform);
            }
            catch (Exception e)
            {
                Con.Error(e);
            }
        }
    }
}