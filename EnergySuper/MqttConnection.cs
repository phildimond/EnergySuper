using System.Text;
using EnergySuper.EventArgsAndHandlers;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace EnergySuper;

public delegate void MqttMessageReceivedHandler(object? sender, MqttMessageReceivedEventArgs e); 
    
public class MqttConnection
{
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _clientOptions;

    /// <summary>
    /// MQTT message received
    /// </summary>
    public event MqttMessageReceivedHandler? MqttMessageReceived;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mqttBroker">URI of the MQTT broker, eg homeassistant.local</param>
    /// <param name="mqttPort">Port the broker is listening on, normally 1883</param>
    /// <param name="mqttUsername">Username</param>
    /// <param name="mqttPassword">Password</param>
    public MqttConnection(string mqttBroker, int mqttPort, string mqttUsername, string mqttPassword)
    {
        var clientIdentifier = Guid.NewGuid().ToString();

        // Create an MQTT client factory
        var factory = new MqttFactory();

        // Create an MQTT client instance
        _mqttClient = factory.CreateMqttClient();

        // Create MQTT client options
        _clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttBroker, mqttPort) // MQTT broker address and port
            .WithCredentials(mqttUsername, mqttPassword) // Set username and password
            .WithClientId(clientIdentifier)
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .Build();
    }

    /// <summary>
    /// Connect to an MQTT broker and subscribe to message received events
    /// </summary>
    /// <returns>null on success, error message on failure</returns>
    public async Task<string?> Connect()
    {
        
        // Connect to the MQTT broker
        var result = await _mqttClient.ConnectAsync(_clientOptions);
        
        Console.WriteLine("MQTT Connection result was " + result.ResultCode);
        if (result.ResultCode != MqttClientConnectResultCode.Success) return result.ReasonString;
        
        _mqttClient.ApplicationMessageReceivedAsync += MqttClientOnApplicationMessageReceivedAsync;

        return null;
    }

    /// <summary>
    /// Process received messages ang generate events
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        string rx = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment);
        
        string topic = arg.ApplicationMessage.Topic;
        string payload = Encoding.ASCII.GetString(arg.ApplicationMessage.PayloadSegment);

        MqttMessageReceived?.Invoke( this,
            new MqttMessageReceivedEventArgs() { Topic = topic, Payload = payload });

        return Task.CompletedTask;
        
    }

    /// <summary>
    /// Disconnect from the MQTT server
    /// </summary>
    public async void Disconnect()
    {
        await _mqttClient.DisconnectAsync();
    }

    /// <summary>
    /// Subscribe to a topic
    /// </summary>
    /// <param name="topic">Full topic name to subscribe to</param>
    /// <returns>null if subscription successful, encountered exception if encountered,
    /// or an application exception with an appropriate message on simple subscription failure</returns>
    public async Task<Exception?> Subscribe(string topic)
    {
        try
        {
            MqttClientSubscribeResult r = await _mqttClient.SubscribeAsync(topic);
            if (r == null || r.Items == null || r.Items.Count == 0) 
                return new ApplicationException($"No result returned from subscription request to topic '{topic}'");
            foreach (var item in r.Items)
                if (item.ResultCode == MqttClientSubscribeResultCode.NotAuthorized) 
                    return new ApplicationException(item.ResultCode.ToString());
        }
        catch (Exception ex) { return ex; }

        return null;
    }
    
    /// <summary>
    /// Send an MQTT message
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="payload"></param>
    /// <param name="retain"></param>
    /// <param name="mqttQualityOfServiceLevel">QoS level, defaults to At Least Once</param>
    /// <returns>null on success, reason message on failure</returns>
    public async Task<string?> SendMessageAsync(string topic, string payload, bool retain = false,
        MqttQualityOfServiceLevel mqttQualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(mqttQualityOfServiceLevel)
            .WithRetainFlag(retain)
            .Build();
        
        var result = await _mqttClient.PublishAsync(message);

        if (!result.IsSuccess)
        {
            return "MQTT transmit failure: " + result.ReasonString;
        }

        return null;
    }
}

