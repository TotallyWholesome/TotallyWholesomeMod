using System.Collections.Generic;
using MessagePack;
using TWNetCommon.Data.NestedObjects;

namespace TWNetCommon.Data.ControlPackets
{
    [MessagePackObject]
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

        [Key(4)] public bool MasterGlobalControl { get; set; }

        [Key(5)] 
        public bool DisableFlight { get; set; }

        [Key(6)]
        public bool DisableSeats { get; set; }
        [Key(7)]
        public TWVector3 LeashPinPosition { get; set; } = TWVector3.Zero;
        [Key(8)]
        public string PropTarget { get; set; } = "";
        [Key(9)]
        public bool BlindPet { get; set; }
        [Key(10)]
        public bool DeafenPet { get; set; }

        public override string ToString()
        {
            var prop = PropTarget ?? "none";
            return $"MasterRemoteControl: [Key: {Key}, GagPet: {GagPet}, ParameterCount: {Parameters?.Count??0}, ParameterConfigureUpdate: {ParameterConfigureUpdate} DisableFlight: {DisableFlight}, DisableSeats: {DisableSeats}, LeashPinPosition: {LeashPinPosition}, PropTarget: {prop}, BlindPet: {BlindPet}, DeafenPet: {DeafenPet}]";
        }
    }
}