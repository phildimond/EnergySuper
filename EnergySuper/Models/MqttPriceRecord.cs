using System.Text.Json.Serialization;

namespace EnergySuper.Models;

public class MqttPriceRecord
{
    /*
        "name": "importPrice",
        "units": "c/kWh",
        "value": 0.07
     */
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("units")]
    public string Units { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}