using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace EnergySuper;

public class MqttConnection
{
    private readonly string _mqttBroker;
    private readonly int _mqttPort;
    private readonly string _mqttUsername;
    private readonly string _mqttPassword;
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _clientOptions;

    public MqttConnection(string mqttBroker, int mqttPort, string mqttUsername, string mqttPassword)
    {
        _mqttBroker = mqttBroker;
        _mqttPort = mqttPort;
        _mqttUsername = mqttUsername;
        _mqttPassword = mqttPassword;
        
        var clientIdentifier = Guid.NewGuid().ToString();

        // Create a MQTT client factory
        var factory = new MqttFactory();

        // Create a MQTT client instance
        _mqttClient = factory.CreateMqttClient();

        // Create MQTT client options
        _clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttBroker, _mqttPort) // MQTT broker address and port
            .WithCredentials(_mqttUsername, _mqttPassword) // Set username and password
            .WithClientId(clientIdentifier)
            .WithCleanSession()
            .Build();
    }

    public async void Connect()
    {
        
        string topic = "homeassistant/CurrentTime";

        // Connect to MQTT broker
        var t1 =
            //Task.Run<MqttClientConnectResult>(async () => await _mqttClient.ConnectAsync(_clientOptions));
            await _mqttClient.ConnectAsync(_clientOptions);

        if (t1.ResultCode == MqttClientConnectResultCode.Success)
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

