using PowerWallLocalApi;
using PowerWallLocalApi.Models;

namespace EnergySuper;

/// <summary>
/// Encapsulates data retrieved from a PowerWall 2 Local API 
/// </summary>
public class PowerWall2LocalData
{
    public AggregateMeterSet MeterSet { get; set; } = new AggregateMeterSet();
}