using MessagePack;

namespace TWNetCommon.Data
{
    [MessagePackObject()]
    public class PairJoinNotification
    {
        [Key(0)]
        public bool Master { get; set; }
        [Key(1)]
        public string DisplayName { get; set; }
    }
}