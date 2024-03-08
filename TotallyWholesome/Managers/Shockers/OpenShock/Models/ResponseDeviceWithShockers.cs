using System.Collections.Generic;

namespace TotallyWholesome.Managers.Shockers.OpenShock.Models;

public class ResponseDeviceWithShockers : ResponseDevice
{
    public IEnumerable<ShockerResponse> Shockers { get; set; }
}