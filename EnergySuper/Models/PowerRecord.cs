using System.Text.Json.Serialization;

namespace EnergySuper.Models;

public class PowerRecord
{
    /*
    {
        "name": "House",
        "units": "kW",
        "value": 1.683
    },
    */
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("units")]
    public string Units { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public double Value { get; set; }
    
}