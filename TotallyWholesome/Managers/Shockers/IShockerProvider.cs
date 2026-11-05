using System.Threading.Tasks;
using TWNetCommon.Data.ControlPackets.Shockers.Models;

namespace TotallyWholesome.Managers.Shockers;

public interface IShockerProvider
{
    /// <summary>
    /// Control all shockers of this shocker provider
    /// </summary>
    /// <param name="type"></param>
    /// <param name="intensity"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public Task Control(ControlType type, byte intensity, ushort duration);
    
    /// <summary>
    /// Does this provider have any shockers enabled?
    /// </summary>
    /// <returns></returns>
    public bool HasShockers();

    // < ---- ACHIEVEMENTS ---- >
    
    /// <summary>
    /// Are the max limits set for the shocker? Or there is none?
    /// </summary>
    public bool NoLimits { get; }
}