using MessagePack;

namespace TWNetCommon.Auth
{
    [MessagePackObject()]
    public class AuthResp
    {
        [Key(0)]
        public string RespMsg { get; set; }
        [Key(1)]
        public int OnlineUsers { get; set; }
        [Key(2)]
        public bool Success { get; set; }
        [Key(3)]
        public bool UpdateLoader { get; set; }

        public override string ToString()
        {
            return $"AuthResp - [RespMsg: {RespMsg}, OnlineUsers: {OnlineUsers}, Success: {Success}, UpdateLoader: {UpdateLoader}]";
        }
    }
}