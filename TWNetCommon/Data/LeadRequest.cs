using System.Collections.Generic;
using MessagePack;
using TWNetCommon.Data.NestedObjects;

namespace TWNetCommon.Data
{
    [MessagePackObject()]
    public class LeadRequest
    {
        [Key(0)]
        public bool MasterRequest { get; set; }
        
        //Initial request
        [Key(1)]
        public string Target{ get; set; }
        [Key(2)]
        public int BoneTarget { get; set; }
        [Key(3)]
        public float LeadLength { get; set; }
        [Key(4)]
        public bool NoVisibleLeash { get; set; }
        [Key(5)]
        public bool PrivateLeash { get; set; }
        [Key(6)]
        public string Key { get; set; }
        //Response data
        [Key(7)]
        public string RequesterID { get; set; } = "";
        //Leash Colour
        [Key(8)]
        public string LeashColour { get; set; } = "";
        [Key(9)]
        public int LeashStyle { get; set; } = -1;
        [Key(10)]
        public bool TempUnlockLeash { get; set; }
        [Key(11)]
        public NetworkedFeature AppliedFeatures { get; set; }

        public override string ToString()
        {
            return $"LeadRequest - [Target: {Target}, BoneTarget: {BoneTarget}, LeadLength: {LeadLength}, NoVisibleLeash: {NoVisibleLeash}, PrivateLeash: {PrivateLeash}, Key: {Key}, RequesterID: {RequesterID}, MasterRequest: {MasterRequest}, LeashColour: {LeashColour}, TempLeashUnlock: {TempUnlockLeash}, AppliedFeatures: {AppliedFeatures}]";
        }
    }
}