using System.Collections.Generic;
using MessagePack;
using TWNetCommon.Data.NestedObjects;

namespace TWNetCommon.Data
{
    [MessagePackObject()]
    public class MasterRemoteControl
    {
        /// <summary>
        /// LeadPair key shared during initial request, if none given packet affects all pets
        /// </summary>
        [Key(0)]
        public string Key { get; set; }

        /// <summary>
        /// Not yet used, will replace ForcedMute in MasterSettingsUpdate
        /// </summary>
        [Key(1)]
        public bool GagPet { get; set; }

        /// <summary>
        /// List of parameters we want to send across, will only send updated parameters instead of everything
        /// </summary>
        [Key(2)]
        public List<MasterRemoteParameter> Parameters { get; set; }

        /// <summary>
        /// Used to tell the master that this packet contains remote parameter information from the pet
        /// </summary>
        [Key(3)]
        public bool ParameterConfigureUpdate { get; set; }

        /// <summary>
        /// ButtplugIO toy strength
        /// </summary>
        [Key(4)]
        public float ToyStrength { get; set; }

        [Key(5)] public bool MasterGlobalControl { get; set; }

        /// <summary>
        /// Duration of a Shoker Vibration, Shock, Beep (PiShock)
        /// </summary>
        [Key(6)]
        public int ShockDuration { get; set; }

        /// <summary>
        /// Strength of a Shoker Vibration, Shock (PiShock)
        /// </summary>
        [Key(7)]
        public int ShockStrength { get; set; }

        /// <summary>
        /// ShockOperation to perform
        /// </summary>
        [Key(8)]
        public ShockOperation ShockOperation { get; set; } = ShockOperation.NoOp;

        /// <summary>
        /// IUs HeightControl enabled
        /// /// </summary>
        [Key(10)]
        public bool ShockHeightEnabled { get; set; }

        /// <summary>
        /// ShockHeight
        /// /// </summary>
        [Key(11)]
        public float ShockHeight { get; set; }

        /// <summary>
        /// Minimum shockstrength on Height Control
        /// /// </summary>
        [Key(12)]
        public float ShockHeightStrengthMin { get; set; }

        /// <summary>
        /// Maximum shockstrength on Height Control
        /// /// </summary>
        [Key(13)]
        public float ShockHeightStrengthMax { get; set; }

        /// <summary>
        /// How fast it goes from Min to Max
        /// /// </summary>
        [Key(14)]
        public float ShockHeightStrengthStep { get; set; }
        
        [Key(15)] 
        public bool DisableFlight { get; set; }

        [Key(16)]
        public bool DisableSeats { get; set; }
        [Key(17)]
        public TWVector3 LeashPinPosition { get; set; } = TWVector3.Zero;
        [Key(18)]
        public string PropTarget { get; set; } = "";

        public override string ToString()
        {
            var prop = PropTarget ?? "none";
            return $"MasterRemoteControl: [Key: {Key}, GagPet: {GagPet}, ParameterCount: {Parameters?.Count??0}, ParameterConfigureUpdate: {ParameterConfigureUpdate}, ToyStrength: {ToyStrength}, ShockDuration: {ShockDuration}, ShockStrengh: {ShockStrength}, ShockOperation: {ShockOperation}, DisableFlight: {DisableFlight}, DisableSeats: {DisableSeats}, LeashPinPosition: {LeashPinPosition}, PropTarget: {prop}]";
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