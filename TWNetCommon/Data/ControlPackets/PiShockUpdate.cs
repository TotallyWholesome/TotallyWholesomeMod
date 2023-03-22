using MessagePack;

namespace TWNetCommon.Data.ControlPackets
{
    [MessagePackObject]
    public class PiShockUpdate
    {
        [Key(0)]
        public string Key { get; set; }
        
        [Key(1)] 
        public bool ShockHeightEnabled { get; set; }

        /// <summary>
        /// ShockOperation to perform
        /// </summary>
        [Key(2)]
        public ShockOperation ShockOperation { get; set; } = ShockOperation.NoOp;

        /// <summary>
        /// Duration of a Shoker Vibration, Shock, Beep (PiShock)
        /// </summary>
        [Key(3)]
        public int ShockDuration { get; set; }

        /// <summary>
        /// Strength of a Shoker Vibration, Shock (PiShock)
        /// </summary>
        [Key(4)]
        public int ShockStrength { get; set; }

        /// <summary>
        /// ShockHeight
        /// /// </summary>
        [Key(5)]
        public float ShockHeight { get; set; }

        /// <summary>
        /// Minimum shockstrength on Height Control
        /// /// </summary>
        [Key(6)]
        public float ShockHeightStrengthMin { get; set; }

        /// <summary>
        /// Maximum shockstrength on Height Control
        /// /// </summary>
        [Key(7)]
        public float ShockHeightStrengthMax { get; set; }

        /// <summary>
        /// How fast it goes from Min to Max
        /// /// </summary>
        [Key(8)]
        public float ShockHeightStrengthStep { get; set; }

        public override string ToString()
        {
            return $"PiShockUpdate - [Key: {Key}, ShockOperation: {ShockOperation}, ShockDuration: {ShockDuration}, ShockStrength: {ShockStrength}, ShockHeightEnabled: {ShockHeightEnabled}, ShockHeight: {ShockHeight}, ShockHeightStrengthMin: {ShockHeightStrengthMin}, ShockHeightStrengthMax: {ShockHeightStrengthMax}, ShockHeightStrengthStep: {ShockHeightStrengthStep}]";
        }
    }
    
    /// <summary>
    /// https://apidocs.pishock.com/#shock-collar-control
    /// </summary>
    public enum ShockOperation
    {
        NoOp = -1,
        Shock = 0,
        Vibrate = 1,
        Beep = 2
    }
}