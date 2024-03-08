namespace TWNetCommon
{
    /// <summary>
    /// TWNet Message Types
    /// 0x0xxx - Auth Types
    /// 0x1xxx - Data Types
    /// </summary>
    public enum TWNetMessageType
    {
        //--------- Auth Types ---------
        Auth = 0x0001,
        AuthResp = 0x0002,
        Disconnection = 0x0003,
        PingPong = 0x0004,
        UserJoin = 0x0005,
        UserLeave = 0x0006,
        

        //--------- Basic Data Types ---------
        SystemNotice = 0x1002,
        UserCountUpdated = 0x1003,
        PairJoinNotification = 0x1004,

        //--------- Function Data Types ---------
        LeadRequest = 0x2000,
        LeadRequestResp = 0x2001,
        LeadAccept = 0x2002,
        InstanceInfo = 0x2003,
        MasterSettings = 0x2005,
        StatusUpdate = 0x2006,
        StatusUpdateConfirmation = 0x2007,
        InstanceFollowResponse = 0x2008,
        LeashConfigUpdate = 0x2009,
        PiShockUpdate = 0x2010,
        ButtplugUpdate = 0x2011,
        MasterRemoteControl3 = 0x2113,
        PetConfigUpdate = 0x2114,
        ShockerControl = 0x2013,
        HeightControl = 0x2014
    }
}