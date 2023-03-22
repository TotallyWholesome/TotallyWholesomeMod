using MessagePack;

namespace TWNetCommon.Data.ControlPackets
{
    [MessagePackObject()]
    public class MasterSettings
    {
        [Key(0)]
        public float LeadLength { get; set; }
        [Key(1)]
        public bool TempUnlockLeash { get; set; }
        [Key(2)]
        public string Key { get; set; }

        public override string ToString()
        {
            return $"MasterSettings - [LeadLength: {LeadLength}, TempUnlockLeash: {TempUnlockLeash}, Key: {Key}]";
        }
    }
}