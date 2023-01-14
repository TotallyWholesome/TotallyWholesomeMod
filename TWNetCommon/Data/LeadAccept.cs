using MessagePack;
using TWNetCommon.Data.NestedObjects;

namespace TWNetCommon.Data
{
    [MessagePackObject()]
    public class LeadAccept
    {
        [Key(0)]
        public string MasterID { get; set; } = "";
        [Key(1)]
        public string FollowerID { get; set; } = "";
        [Key(2)]
        public int PetBoneTarget { get; set; } = -1;
        [Key(3)]
        public int MasterBoneTarget { get; set; } = -1;
        [Key(4)]
        public float LeadLength { get; set; } = 0f;
        [Key(5)]
        public bool ForcedMute { get; set; } = false;
        [Key(6)]
        public bool NoVisibleLeash { get; set; }
        [Key(7)]
        public bool PrivateLeash { get; set; }
        [Key(8)]
        public string Key { get; set; }
        [Key(9)]
        public bool FollowerAccept { get; set; }
        [Key(10)]
        public bool LeadRemove { get; set; }
        [Key(11)]
        public bool TempUnlockLeash { get; set; }
        //Master leash colour
        [Key(12)]
        public string MasterLeashColour { get; set; } = "";
        //Pet leash colour
        [Key(13)]
        public string PetLeashColour { get; set; } = "";
        [Key(14)]
        public TWVector3 LeashPinPosition { get; set; } = TWVector3.Zero;
        [Key(15)]
        public string PropTarget { get; set; } = "";
        [Key(16)]
        public int LeashStyle { get; set; }
        [Key(17)] 
        public bool DisableFlight { get; set; }
        [Key(18)] 
        public bool DisableSeats { get; set; }

        public override string ToString()
        {
            return $"LeadAccept - [MasterID: {MasterID}, MasterBoneTarget: {MasterBoneTarget}, LeadLength: {LeadLength}, ForcedMute: {ForcedMute}, NoVisibleLeash: {NoVisibleLeash}, PrivateLeash: {PrivateLeash}, PetID: {FollowerID}, PetBoneTarget: {PetBoneTarget}, FollowerRequest: {FollowerAccept}, LeadRemove: {LeadRemove}, TempUnlockLeash: {TempUnlockLeash}, Key: {Key}, MasterLeashColour: {MasterLeashColour}, PetLeashColour: {PetLeashColour}, DisableFlight: {DisableFlight}, DisableSeats: {DisableSeats}]";
        }
    }
}