namespace EnergySuper.EventArgsAndHandlers;

/// <summary>
/// Arguments for an MQTT received message event
/// </summary>
public class MqttMessageReceivedEventArgs
{
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}