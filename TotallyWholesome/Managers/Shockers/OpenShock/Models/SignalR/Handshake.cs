namespace TotallyWholesome.Managers.Shockers.OpenShock.Models.SignalR;

public class Handshake
{
    public string Protocol { get; set; } = null!;
    public uint Version { get; set; }
}