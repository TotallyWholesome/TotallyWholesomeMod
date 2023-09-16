
using MessagePack;

namespace TWNetCommon.Auth
{
    [MessagePackObject]
    public class Auth
    {
        [Key(0)]
        public string UserID { get; set; }
        [Key(1)]
        public string DisplayName { get; set; }
        [Key(2)]
        public string TWVersion { get; set; }
        [Key(3)]
        public string Key { get; set; }
        [Key(4)]
        public string WLVersion { get; set; }

        public override string ToString()
        {
            return $"AuthPacket - [UserID: {UserID}, DisplayName: {DisplayName}, TWVersion: {TWVersion}, Key: {Key}, WLVersion: {WLVersion}]";
        }
    }
}