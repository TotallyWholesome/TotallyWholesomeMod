using MessagePack;

namespace TWNetCommon.Data
{
    [MessagePackObject()]
    public class StatusUpdateConfirmation
    {
        [Key(0)]
        public bool StatusApplyFailure;
        [Key(1)]
        public string ApplyMessage;
    }
}