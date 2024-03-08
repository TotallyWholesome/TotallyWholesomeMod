using System.Collections.Generic;
using TWNetCommon.Data.ControlPackets;
using TWNetCommon.Data.ControlPackets.Shockers.Models;

namespace TotallyWholesome.Managers.Shockers.PiShock;

public static class PiShockUtils
{
    public static readonly IReadOnlyDictionary<ControlType, ShockOperation> OperationTranslation =
        new Dictionary<ControlType, ShockOperation>
        {
            { ControlType.Shock, ShockOperation.Shock },
            { ControlType.Vibrate, ShockOperation.Vibrate },
            { ControlType.Sound, ShockOperation.Beep }
        };
}