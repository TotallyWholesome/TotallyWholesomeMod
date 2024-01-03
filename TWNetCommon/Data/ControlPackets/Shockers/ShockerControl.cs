#nullable enable
using System;
using MessagePack;
using TWNetCommon.Data.ControlPackets.Shockers.Models;

namespace TWNetCommon.Data.ControlPackets.Shockers;

[MessagePackObject]
public class ShockerControl
{
    /// <summary>
    /// Target lead pair, to target a specific pet
    /// </summary>
    [Key(0)]
    public string? Key { get; set; }
    
    /// <summary>
    /// Intensity of the the action
    /// 1 - 100
    /// </summary>
    [Key(1)]
    public byte Intensity { get; set; }
    
    /// <summary>
    /// Duration of the Shock in millis
    /// 300 - 30_000
    /// </summary>
    [Key(2)]
    public uint Duration { get; set; }
    
    /// <summary>
    /// Action type
    /// </summary>
    [Key(3)]
    public ControlType Type { get; set; }
    
    /// <summary>
    /// Optional shocker id, to target a specific shocker. If null the receiver decides which shocker to use.
    /// (mode can be random or all, of the active shockers)
    /// </summary>
    [Key(4)]
    public Guid? Id { get; set; }

    public override string ToString() => $"ShockControl - Key: [{Key}] Intensity: [{Intensity}] Duration: [{Duration}] Type: [{Type}] Id: [{Id}]";
}