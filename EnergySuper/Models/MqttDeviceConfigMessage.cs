using System.Text.Json.Serialization;

namespace EnergySuper.Models;

/*
{
    "unique_id": "T_dfghsfgfsdfhgh98734tie",
    "device": {
        "identifiers": [
        "EnphaseRelays-ID"
            ],
        "name": "EnphaseRelays"
    },
    "availability": {
        "topic": "homeassistant/number/EnphaseRelays/availability",
        "payload_available": "online",
        "payload_not_available": "offline"
    },
    "min": 0,
    "max": 15,
    "retain": true,
    "command_topic": "homeassistant/number/EnphaseRelays/command",
    "state_topic": "homeassistant/number/EnphaseRelays/command"
}
*/

public class MqttDeviceConfigMessageDevice(string[] identifiers, string name)
{
    [JsonPropertyName("identifiers")]
    public string[] Identifiers { get; set; } = identifiers;

    [JsonPropertyName("name")]
    public string Name { get; set; } = name;
}

public class MqttDeviceAvailabilityConfigMessage(string topic, string availableMessage, string unavailableMessage)
{
    [JsonPropertyName("topic")]
    public string topic { get; set; } = topic;
    
    [JsonPropertyName("payload_available")]
    public string AvailableMessage { get; set; } = availableMessage;
    
    [JsonPropertyName("payload_not_available")]
    public string UnavailableMessage { get; set; } = unavailableMessage;
        
}
public class MqttDeviceConfigMessage(
    string name, string uniqueId, string[] identifiers, string deviceName,
    string availabilityTopic, string availableMessage, string unavailableMessage,
    string deviceClass, string stateTopic, string commandTopic = "", string mode = "auto",
    string? unitOfMeasurement = null, double min = 0, double max = 100, double step = 1, bool retain = false)
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = name;
    
    [JsonPropertyName("unique_id")]
    public string UniqueId { get; set; } = uniqueId;
    
    [JsonPropertyName("device")]
    public MqttDeviceConfigMessageDevice Device { get; set; } = new(identifiers, deviceName);
    
    [JsonPropertyName("availability")]
    public MqttDeviceAvailabilityConfigMessage Availability { get; set; } = new(availabilityTopic, availableMessage, unavailableMessage);

    [JsonPropertyName("device_class")]
    public string DeviceClass { get; set; } = deviceClass;
    
    [JsonPropertyName("command_topic")]
    public string CommandTopic { get; set; } = commandTopic;
    
    [JsonPropertyName("state_topic")]
    public string StateTopic { get; set; } = stateTopic;
    
    [JsonPropertyName("unit_of_measurement")]
    public string? UnitOfMeasurement { get; set; } = unitOfMeasurement;
    
    [JsonPropertyName("min")]
    public double Min { get; set; } = min;
    
    [JsonPropertyName("max")]
    public double Max { get; set; } = max;
    
    [JsonPropertyName("step")]
    public double Step { get; set; } = step;
    
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = mode;
    
    [JsonPropertyName("retain")]
    public bool Retain { get; set; } = retain;
    
}