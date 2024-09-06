using System.Text.Json.Serialization;

namespace EnergySuper.Models;

public class MqttPowerMessage
{
    /*
    {
        "batteryLevel": 46,
        "currentPrices": [
        {
            "name": "importPrice",
            "units": "c/kWh",
            "value": 0.07
        },
        {
            "name": "exportPrice",
            "units": "c/kWh",
            "value": -0.01
        },
        {
            "name": "controlledLoadPrice",
            "units": "c/kWh",
            "value": 0.07
        }
        ],
        "forecastPrices": [
        {
            "name": "importPrice",
            "units": "c/kWh",
            "value": 0.05
        },
        {
            "name": "exportPrice",
            "units": "c/kWh",
            "value": -0.03
        },
        {
            "name": "controlledLoadPrice",
            "units": "c/kWh",
            "value": 0.05
        }
        ],
        "powerValues": [
        {
            "name": "House",
            "units": "kW",
            "value": 1.683
        },
        {
            "name": "Solar",
            "units": "kW",
            "value": 6.952
        },
        {
            "name": "Battery",
            "units": "kW",
            "value": -5.29
        },
        {
            "name": "Grid",
            "units": "kW",
            "value": -0.033
        },
        {
            "name": "GridImportToday",
            "units": "kW",
            "value": 3.46
        },
        {
            "name": "GridExportToday",
            "units": "kW",
            "value": 0.49
        }
        ]
    }
    */
    
    [JsonPropertyName("batteryLevel")]
    public double BatteryLevel { get; set; }
    
    [JsonPropertyName("currentPrices")]
    public MqttPriceRecord[]? CurrentPrices { get; set; }
    
    [JsonPropertyName("forecastPrices")]
    public MqttPriceRecord[]? ForecastPrices { get; set; }

    [JsonPropertyName("powerValues")]
    public PowerRecord[]? PowerRecords { get; set; }
}