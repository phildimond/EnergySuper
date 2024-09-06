using AmberElectricityAPI.Models;

namespace EnergySuper.Models;

/// <summary>
/// Encapsulates information we've obtained about Amber Electricity
/// </summary>
public class AmberData
{
    /// <summary>
    /// Contains all price records for today's calendar date
    /// </summary>
    public List<IntervalRecord> PricesToday { get; set; } = new List<IntervalRecord>();
}