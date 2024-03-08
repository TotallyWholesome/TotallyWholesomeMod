using System;
using TotallyWholesome.Managers.Shockers;
using TWNetCommon.Data.ControlPackets.Shockers.Models;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class ShockerActionPetCondition: Attribute, ICondition
{
    private readonly ControlType _controlType;
    private readonly byte _intensity;
    private readonly uint _duration;
    private DateTime _lastPetFired;

    public bool CheckCondition()
    {
        if (ShockerManager.Instance.LastActionTimePet == _lastPetFired) return false;
        if (ShockerManager.Instance.LastControlTypePet != _controlType) return false;
        if (ShockerManager.Instance.LastDurationPet < _duration) return false;
        if (ShockerManager.Instance.LastIntensityPet < _intensity) return false;

        _lastPetFired = ShockerManager.Instance.LastActionTimePet;

        return true;
    }

    /// <summary>
    /// Check for a pet receiving a shock with a specific type, higher intensity and higher duration
    /// </summary>
    /// <param name="controlType"></param>
    /// <param name="intensity"></param>
    /// <param name="duration"></param>
    public ShockerActionPetCondition(ControlType controlType, byte intensity, uint duration)
    {
        _controlType = controlType;
        _intensity = intensity;
        _duration = duration;
    }
}