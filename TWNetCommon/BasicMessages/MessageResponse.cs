using MessagePack;

namespace TWNetCommon.BasicMessages
{
    [MessagePackObject]
    public class MessageResponse
    {
        [Key(0)]
        public string Message { get; set; }

        public override string ToString()
        {
            return $"MessageResponse - [Message: {Message}]";
        }
    }
}