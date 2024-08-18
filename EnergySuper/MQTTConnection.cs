using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace EnergySuper;

public class MQTTConnection
{
    public string MqttBroker { get; set;} = string.Empty;
    public int MqttPort { get; set; } = 1883;
    public string MqttUsername { get; set; } = string.Empty;
    public string MqttPassword { get; set; } = string.Empty;

    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _clientOptions;

    public MQTTConnection(string mqttBroker, int mqttPort, string mqttUsername, string mqttPassword)
    {
        MqttBroker = mqttBroker;
        MqttPort = mqttPort;
        MqttUsername = mqttUsername;
        MqttPassword = mqttPassword;
        
        var clientIdentifier = Guid.NewGuid().ToString();

        // Create a MQTT client factory
        var factory = new MqttFactory();

        // Create a MQTT client instance
        _mqttClient = factory.CreateMqttClient();

        // Create MQTT client options
        _clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(MqttBroker, MqttPort) // MQTT broker address and port
            .WithCredentials(MqttUsername, MqttPassword) // Set username and password
            .WithClientId(clientIdentifier)
            .WithCleanSession()
            .Build();
    }

    public void GetSettingsFromConsole()
    {
        Console.WriteLine("Need to configure the MQTT broker");
        
        Console.Write("MQTT Broker: ");
        string? s = Console.ReadLine();
        if (!string.IsNullOrEmpty(s)) MqttBroker = s;
        else throw new ApplicationException("MQTT Broker is required");
        
        Console.Write("MQTT Port: ");
        s = Console.ReadLine();
        if (!string.IsNullOrEmpty(s)) MqttPort = int.Parse(s);
        else throw new ApplicationException("MQTT Port is required");
        
        Console.Write("MQTT Username: ");
        s = Console.ReadLine();
        if (!string.IsNullOrEmpty(s)) MqttUsername = s;
        else throw new ApplicationException("MQTT Username is required");

        Console.Write("MQTT Password: ");
        s = Console.ReadLine();
        if (!string.IsNullOrEmpty(s)) MqttPassword = s;
        else throw new ApplicationException("MQTT Password is required");
    }

    public async void Connect()
    {
        
        string topic = "homeassistant/CurrentTime";

        // Connect to MQTT broker
        var t1 =
            Task.Run<MqttClientConnectResult>(async () => await _mqttClient.ConnectAsync(_clientOptions));

        if (t1.Result.ResultCode == MqttClientConnectResultCode.Success)
        {
            Console.WriteLine("Connected to MQTT broker successfully.");

            // Subscribe to a topic
            var t2 =
                Task.Run<MqttClientSubscribeResult>(async () => await _mqttClient.SubscribeAsync(topic));

            // Callback function when a message is received
            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                string rx = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                Console.Write($"Received message: {rx}");
                Console.WriteLine("   ...Press enter to exit the program.");

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

                return Task.CompletedTask;
            };
        }
    }

    public async void Disconnect()
    {
        await _mqttClient.DisconnectAsync();
    }
}

