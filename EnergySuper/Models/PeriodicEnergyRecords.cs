namespace EnergySuper.Models;

/// <summary>
/// Encapsulates values of various meters at periodic intervals.
/// Reference period is 00:00:00 (ie midnight local time)
/// </summary>
public class PeriodicEnergyRecords
{
    public DateTime DateTimeTaken { get; set; } = DateTime.MinValue;
    public double ControlledLoadGridImport { get; set; }
    public double GeneralGridImport { get; set; }
    public double GeneralGridExport { get; set; }
    public double BatteryImport { get; set; }
    public double BatteryExport { get; set; }
    public double SolarGenerated { get; set; }
    public double HouseLoadImport { get; set; }
    public double BatteryChargeState { get; set; }
    public double BatteryEnergyCost  { get; set; }
}