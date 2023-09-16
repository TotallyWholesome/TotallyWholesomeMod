using MessagePack;

namespace TWNetCommon.Data
{
    [MessagePackObject]
    public class InstanceFollowResponse
    {
        [Key(0)]
        public string Key;
        [Key(1)]
        public string UserID;
        [Key(2)]
        public string TargetInstanceHash;
    }
}