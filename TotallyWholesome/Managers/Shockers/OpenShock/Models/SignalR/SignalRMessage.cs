using System.Collections.Generic;
using JetBrains.Annotations;

namespace TotallyWholesome.Managers.Shockers.OpenShock.Models.SignalR;

public class SignalRMessage
{
    public MessageType Type { get; set; } 
    [CanBeNull] public string Target { get; set; }
    [CanBeNull] public IList<object> Arguments { get; set; }
}

public sealed class SignalRServerMessage : SignalRMessage
{
    [CanBeNull] public string Error { get; set; }
}