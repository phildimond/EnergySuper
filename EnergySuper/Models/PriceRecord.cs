using System.Text.Json.Serialization;

namespace EnergySuper.Models;

public class PriceRecord
{
    /*
        "name": "importPrice",
        "units": "c/kWh",
        "value": 0.07
     */
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("units")]
    public string Units { get; set; }
    
    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}