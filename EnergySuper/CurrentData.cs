namespace EnergySuper;

/// <summary>
/// Encapsulates the current live data used by the app
/// </summary>
public class CurrentData
{
    public DateTime LastPowerUpdate { get; set; } = DateTime.MinValue;
    public double LoadPowerKw { get; set; } = 0.0;
    public double SolarPowerKw { get; set; } = 0.0;
    public double BatteryPowerKw { get; set; } = 0.0;
    public double BatteryChargePercent { get; set; } = 0.0;
    public double GridPowerKw { get; set; } = 0.0;
    public DateTime LastPriceUpdate { get; set; } = DateTime.MinValue;
    public double CurrentPowerPriceBuy { get; set; } = 0.0;
    public double CurrentPowerPriceSell { get; set; } = 0.0;
    public double CurrentPowerPriceControlledLoad { get; set; } = 0.0;
    public double ForecastPowerPriceBuy { get; set; } = 0.0;
    public double ForecastPowerPriceSell { get; set; } = 0.0;
    public double ForecastPowerPriceControlledLoad { get; set; } = 0.0;
}