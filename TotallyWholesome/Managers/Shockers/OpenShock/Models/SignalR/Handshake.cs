namespace TotallyWholesome.Managers.Shockers.OpenShock.Models.SignalR;

public sealed class Handshake
{
    public string Protocol { get; set; } = null!;
    public uint Version { get; set; }
}