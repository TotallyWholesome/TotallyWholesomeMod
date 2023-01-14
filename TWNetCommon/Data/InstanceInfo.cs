using MessagePack;

namespace TWNetCommon.Data
{
    [MessagePackObject]
    public class InstanceInfo
    {
        [Key(0)]
        public string FullInstanceID { get; set; }
        [Key(1)]
        public string InstanceHash { get; set; }

        public override string ToString()
        {
            return $"InstanceInfo - [FullInstanceID: {FullInstanceID}, InstanceHash: {InstanceHash}]";
        }
    }
}