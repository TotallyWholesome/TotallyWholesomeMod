namespace TWNetCommon
{
    /// <summary>
    /// TWNet Message Types
    /// 0x0xxx - Auth Types
    /// 0x1xxx - Data Types
    /// </summary>
    public static class TWNetMessageTypes
    {
        //TWNet Auth Types
        //----------------
        
        /// <summary>
        /// Announces the user in the TW Network
        /// </summary>
        public const int Auth = 0x0001;
        public const int AuthResp = 0x0002;
        public const int Disconnection = 0x0003;
        public const int PingPong = 0x0004;
        public const int UserJoin = 0x0005;
        public const int UserLeave = 0x0006;

        //TWNet Simple Data Types
        //----------------

        /// <summary>
        /// Sends a broadcast triggering a VRC Hud Message
        /// </summary>
        public const int SystemNotice = 0x1002;

        /// <summary>
        /// Sent on user connection/disconnection to keep clients aware of how many are online
        /// </summary>
        public const int UserCountUpdated = 0x1003;

        /// <summary>
        /// Sent when a known pet or master joins the instance
        /// </summary>
        public const int PairJoinNotification = 0x1004;
        
        //TWNet Function Data Types
        public const int LeadRequest = 0x2000;
        public const int LeadRequestResp = 0x2001;
        public const int LeadAccept = 0x2002;
        public const int InstanceInfo = 0x2003;
        public const int MasterSettings = 0x2005;
        public const int StatusUpdate = 0x2006;
        public const int StatusUpdateConfirmation = 0x2007;
        public const int InstanceFollowResponse = 0x2008;
        public const int LeashConfigUpdate = 0x2009;
        public const int PiShockUpdate = 0x2010;
        public const int ButtplugUpdate = 0x2011;
        public const int MasterRemoteControl2 = 0x2012;
    }
}