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
        public bool ForcedMute { get; set; }
        [Key(5)]
        public bool NoVisibleLeash { get; set; }
        [Key(6)]
        public bool PrivateLeash { get; set; }
        [Key(7)]
        public string Key { get; set; }
        
        //Response data
        [Key(8)]
        public string RequesterID { get; set; } = "";
        //Leash Colour
        [Key(9)]
        public string LeashColour { get; set; } = "";
        [Key(10)]
        public int LeashStyle { get; set; } = -1;
        [Key(11)] 
        public bool DisableFlight { get; set; }
        [Key(12)] 
        public bool DisableSeats { get; set; }
        [Key(13)]
        public bool TempUnlockLeash { get; set; }
        [Key(14)]
        public bool BlindPet { get; set; }
        [Key(15)]
        public bool DeafenPet { get; set; }

        public override string ToString()
        {
            return $"LeadRequest - [Target: {Target}, BoneTarget: {BoneTarget}, LeadLength: {LeadLength}, ForcedMute: {ForcedMute}, NoVisibleLeash: {NoVisibleLeash}, PrivateLeash: {PrivateLeash}, Key: {Key}, RequesterID: {RequesterID}, MasterRequest: {MasterRequest}, LeashColour: {LeashColour}, DisableFlight: {DisableFlight}, DisableSeats: {DisableSeats}, TempLeashUnlock: {TempUnlockLeash}, BlindPet: {BlindPet}, DeafenPet: {DeafenPet}]";
        }
    }
}