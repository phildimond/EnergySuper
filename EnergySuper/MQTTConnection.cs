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
            .Build();
    }

    public void Connect()
    {
        
        // Connect to the MQTT broker
        var t1 = Task.Run<MqttClientConnectResult>(async () => await _mqttClient.ConnectAsync(_clientOptions));
        
        Console.WriteLine("MQTT Connection result was " + t1.Result.ResultCode);
            
        _mqttClient.ApplicationMessageReceivedAsync += MqttClientOnApplicationMessageReceivedAsync;
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
        
        if (MqttMessageReceived != null) MqttMessageReceived.Invoke( this,
            new MqttMessageReceivedEventArgs() { Topic = topic, Payload = payload });
        /*
        Console.Write($"Received message: {rx}");
        Console.WriteLine("\t... press Control-C to exit the program.");
        */
        
        /*
        var message = new MqttApplicationMessageBuilder()
            .WithTopic("homeassistant/my_code_response")
            .WithPayload($"Hello, MQTT! I got the time as " + rx)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var t3 =
            Task.Run<MqttClientPublishResult>(async () => await _mqttClient.PublishAsync(message));

        if (!t3.Result.IsSuccess)
        {
            Console.WriteLine("MQTT transmit failure: " + t3.Result.ReasonString);
        }
        */
        
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
            {
                Console.WriteLine($"\tItem TopicFilter={item.TopicFilter} ResultCode={item.ResultCode}");
                if (item.ResultCode == MqttClientSubscribeResultCode.NotAuthorized) 
                    return new ApplicationException(item.ResultCode.ToString());
            }
        }
        catch (Exception ex) { return ex; }

        return null;
    }
}

