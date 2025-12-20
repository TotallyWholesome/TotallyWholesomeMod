using System;

namespace TotallyWholesome.Managers.Shockers.OpenShock.Models;

public class MinimalShocker
{
    public Guid Id { get; set; }
    public ushort RfId { get; set; }
}

public class ShockerResponse : MinimalShocker
{
    public string Name { get; set; }
    public bool IsPaused { get; set; }
    public DateTime CreatedOn { get; set; }
}