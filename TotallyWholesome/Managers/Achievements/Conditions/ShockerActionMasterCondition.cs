using System;
using TotallyWholesome.Managers.Shockers;
using TWNetCommon.Data.ControlPackets.Shockers.Models;

namespace TotallyWholesome.Managers.Achievements.Conditions;

public class ShockerActionMasterCondition : Attribute, ICondition
{
    private readonly ControlType _action;
    private readonly byte _intensity;
    private readonly uint _duration;
    private DateTime _lastMasterFired;
        
    public bool CheckCondition()
    {
        if (ShockerManager.Instance.LastActionTimeMaster == _lastMasterFired) return false;
        
        if (ShockerManager.Instance.LastControlTypeMaster != _action) return false;
        if (ShockerManager.Instance.LastDurationMaster < _duration) return false;
        if (ShockerManager.Instance.LastIntensityMaster < _intensity) return false;

        _lastMasterFired = ShockerManager.Instance.LastActionTimeMaster;

        return true;
    }

    /// <summary>
    /// Check if a master send a action with a specific type, higher intensity and higher duration
    /// </summary>
    /// <param name="action"></param>
    /// <param name="intensity"></param>
    /// <param name="duration"></param>
    public ShockerActionMasterCondition(ControlType action, byte intensity, uint duration)
    {
        _action = action;
        _intensity = intensity;
        _duration = duration;
    }
}