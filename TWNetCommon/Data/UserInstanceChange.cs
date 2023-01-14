using MessagePack;

namespace TWNetCommon.Data
{
    [MessagePackObject()]
    public class UserInstanceChange
    {
        [Key(0)]
        public string UserIDHash { get; set; }
        
        public override string ToString()
        {
            return $"UserInstanceChange: {UserIDHash}";
        }
    }
}