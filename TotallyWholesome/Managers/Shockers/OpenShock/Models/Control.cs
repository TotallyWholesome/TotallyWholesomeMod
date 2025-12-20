using System;
using TWNetCommon.Data.ControlPackets.Shockers.Models;

namespace TotallyWholesome.Managers.Shockers.OpenShock.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Control
{
    public Guid Id { get; set; }
    public ControlType Type { get; set; }
    public byte Intensity { get; set; }
    public ushort Duration { get; set; }
}