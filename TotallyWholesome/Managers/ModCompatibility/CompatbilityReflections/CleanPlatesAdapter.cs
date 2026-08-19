using System;
using System.Linq;
using System.Reflection;
using ABI_RC.Core.Player;
using MelonLoader;
using TotallyWholesome.Managers.Status;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome.Managers.ModCompatibility.CompatbilityReflections;

public static class CleanPlatesAdapter
{
    private static bool? _cleanPlatesAvailable;
    private static MethodInfo _getPlateCornerMethod;
    private static MethodInfo _getPlateGraphicMatMethod;
    private static EventInfo _plateAttachedEvent;
    private static EventInfo _plateDetachedEvent;
    private static bool _methodsGetRan;

    public static bool IsCleanPlatesAvailable()
    {
        _cleanPlatesAvailable ??= MelonMod.RegisteredMelons.Any(x => x.Info.Name.Equals("CleanPlates"));
        return _cleanPlatesAvailable.Value;
    }

    public static bool GetCleanPlatesMethods()
    {
        if (!IsCleanPlatesAvailable()) return false;
        if (_methodsGetRan) return true;

        Type cleanPlatesAPI = Type.GetType("NAK.CleanPlates.ThirdpartySupport, CleanPlates");

        if (cleanPlatesAPI == null)
        {
            Con.Error("Unable to retrieve CleanPlates API, support for this mod will be unavailable!");
            _cleanPlatesAvailable = false;
            return false;
        }

        try
        {
            _getPlateCornerMethod = cleanPlatesAPI.GetMethod("GetPlateCorner", BindingFlags.Static | BindingFlags.Public);
            if (_getPlateCornerMethod == null) throw new Exception("Failed to retrieve GetPlateCorner method!");
            _getPlateGraphicMatMethod = cleanPlatesAPI.GetMethod("GetPlateGraphicMaterial", BindingFlags.Static | BindingFlags.Public);
            if (_getPlateGraphicMatMethod == null) throw new Exception("Failed to retrieve GetPlateGraphicMaterial method!");
            _plateAttachedEvent = cleanPlatesAPI.GetEvent("PlateAttached", BindingFlags.Static | BindingFlags.Public);
            if (_plateAttachedEvent == null) throw new Exception("Failed to retrieve PlateAttached event!");
            _plateDetachedEvent = cleanPlatesAPI.GetEvent("PlateDetached", BindingFlags.Static | BindingFlags.Public);
            if (_plateDetachedEvent == null) throw new Exception("Failed to retrieve PlateDetached event!");
        }
        catch (Exception)
        {
            Con.Error("Failed to retrieve CleanPlates API Methods, support for this mod will be unavailable!");
            _cleanPlatesAvailable = false;
            return false;
        }
        
        _plateAttachedEvent.AddEventHandler(null, Delegate.CreateDelegate(_plateAttachedEvent.EventHandlerType, typeof(StatusManager).GetMethod(nameof(StatusManager.CleanPlatesPlateAttached), BindingFlags.Public | BindingFlags.Static)));
        _plateDetachedEvent.AddEventHandler(null, Delegate.CreateDelegate(_plateDetachedEvent.EventHandlerType, typeof(StatusManager).GetMethod(nameof(StatusManager.CleanPlatesPlateDetached), BindingFlags.Public | BindingFlags.Static)));
        
        Con.Debug("Successfully retrieved CleanPlates API methods and event via reflection!");
        _methodsGetRan = true;

        return true;
    }

    public static Transform GetPlateCorner(PlayerBase player, int corner)
    {
        if (!GetCleanPlatesMethods()) return null;
        
        return (Transform)_getPlateCornerMethod.Invoke(null, [player, corner]);
    }

    public static Material GetPlateGraphicMat(bool localPlayer)
    {
        if (!GetCleanPlatesMethods()) return null;
        
        return (Material)_getPlateGraphicMatMethod.Invoke(null, [localPlayer]);
    }
}