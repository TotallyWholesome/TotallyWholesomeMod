using MessagePack;

namespace TWNetCommon.Data.ControlPackets
{
    [MessagePackObject]
    public class ButtplugUpdate
    {
        /// <summary>
        /// LeadPair key shared during initial request, if none given packet affects all pets
        /// </summary>
        [Key(0)]
        public string Key { get; set; }
        
        /// <summary>
        /// ButtplugIO toy strength
        /// </summary>
        [Key(1)]
        public float ToyStrength { get; set; }

        public override string ToString()
        {
            return $"ButtplugUpdate - [Key: {Key}, ToyStrength: {ToyStrength}]";
        }

        public ButtplugUpdate Clone()
        {
            return (ButtplugUpdate)MemberwiseClone();
        }
    }
}