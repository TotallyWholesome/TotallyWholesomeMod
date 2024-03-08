#nullable enable
using MessagePack;

namespace TWNetCommon.Data.ControlPackets.Shockers;

/// <summary>
/// Update about height control
/// </summary>
[MessagePackObject]
public sealed class HeightControl
{
    /// <summary>
    /// Target lead pair, to target a specific pet
    /// </summary>
    [Key(0)]
    public string? Key { get; set; }
    
    /// <summary>
    /// Is shock height enabled
    /// </summary>
    [Key(1)] 
    public bool Enabled { get; set; }
    
    /// <summary>
    /// ShockHeight
    /// /// </summary>
    [Key(2)]
    public float Height { get; set; }
    
    /// <summary>
    /// Minimum shock strength on Height Control
    /// /// </summary>
    [Key(3)]
    public float StrengthMin { get; set; }
    
    /// <summary>
    /// Maximum shock strength on Height Control
    /// /// </summary>
    [Key(4)]
    public float StrengthMax { get; set; }
    
    /// <summary>
    /// How fast it goes from Min to Max
    /// /// </summary>
    [Key(5)]
    public float StrengthStep { get; set; }

    public override string ToString() =>
        $"HeightControl - Key: [{Key}] Enabled: [{Enabled}] Height: [{Height}] StrengthMin: [{StrengthMin}] StrengthMax: [{StrengthMax}] StrengthStep: [{StrengthStep}]";

    public HeightControl Clone()
    {
        return (HeightControl)MemberwiseClone();
    }
}