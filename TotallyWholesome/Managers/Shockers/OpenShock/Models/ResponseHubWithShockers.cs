using System.Collections.Generic;

namespace TotallyWholesome.Managers.Shockers.OpenShock.Models;

public sealed class ResponseHubWithShockers : ResponseHub
{
    public IEnumerable<ShockerResponse> Shockers { get; set; }
}