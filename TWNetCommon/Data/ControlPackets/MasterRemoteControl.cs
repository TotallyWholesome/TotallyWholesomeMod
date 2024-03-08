using System;
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
        /// List of parameters we want to send across, will only send updated parameters instead of everything
        /// </summary>
        [Key(1)]
        public List<MasterRemoteParameter> Parameters { get; set; }

        [Key(2)] public bool MasterGlobalControl { get; set; }

        [Key(3)]
        public TWVector3 LeashPinPosition { get; set; } = TWVector3.Zero;
        [Key(4)]
        public string PropTarget { get; set; } = "";
        [Key(5)]
        public string TargetAvatar { get; set; } = null;
        [Key(6)]
        public NetworkedFeature AppliedFeatures { get; set; }

        public override string ToString()
        {
            var prop = PropTarget ?? "none";
            return $"MasterRemoteControl: [Key: {Key}, ParameterCount: {Parameters?.Count??0}, LeashPinPosition: {LeashPinPosition}, PropTarget: {prop}, TargetAvatar: {TargetAvatar}, AppliedFeatures: {AppliedFeatures}]";
        }

        public MasterRemoteControl Clone()
        {
            return (MasterRemoteControl)MemberwiseClone();
        }
    }
}