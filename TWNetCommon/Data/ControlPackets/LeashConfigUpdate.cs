using MessagePack;
using TWNetCommon.Data.NestedObjects;

namespace TWNetCommon.Data.ControlPackets
{
    /// <summary>
    /// This is a broadcasted packet containing anything that should be able to be updated while the leash is active
    /// </summary>
    [MessagePackObject]
    public class LeashConfigUpdate
    {
        [Key(0)]
        public string Key { get; set; }
        [Key(1)]
        public string MasterLeashColour { get; set; } = "";
        [Key(2)]
        public string PetLeashColour { get; set; } = "";
        [Key(3)]
        public TWVector3 LeashPinPosition { get; set; } = TWVector3.Zero;
        [Key(4)]
        public string PropTarget { get; set; } = "";
        [Key(5)] 
        public int LeashStyle { get; set; } = -1;

        public override string ToString()
        {
            var prop = PropTarget ?? "none";
            return $"LeashConfigUpdate - [Key: {Key}, MasterLeashColour: {MasterLeashColour}, PetLeashColour: {PetLeashColour}, LeashStyle: {LeashStyle}, LeashPinPosition: {LeashPinPosition}, PropTarget: {prop}]";
        }
    }
}