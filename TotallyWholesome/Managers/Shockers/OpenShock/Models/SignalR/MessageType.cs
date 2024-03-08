namespace TotallyWholesome.Managers.Shockers.OpenShock.Models.SignalR;

public enum MessageType
{
    Invocation = 1,
    StreamItem = 2,
    Completion = 3,
    StreamInvocation = 4,
    CancelInvocation = 5,
    Ping = 6,
    Close = 7
}