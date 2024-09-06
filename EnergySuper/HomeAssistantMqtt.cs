using System.Text.Json;
using EnergySuper.EventArgsAndHandlers;
using EnergySuper.Models;

namespace EnergySuper;

public class HomeAssistantMqtt (Settings? settings, MqttConnection? mqttConnection)
{
    private Settings? _settings = settings;
    private MqttConnection? _mqttConnection = mqttConnection;
    
    public event LogAvailableEventHandler? LogMessageAvailableEvent;

    /// <summary>
    /// Start the Home Assistant MQTT connection
    /// </summary>
    /// <returns></returns>
    public async Task<bool> Start()
    {
        if (_settings == null)
        {
            LogMessage(LogLevel.Critical, "HomeAssistantMqtt start failed: settings is null");
            return false;
        }

        if (_mqttConnection == null)
        {
            LogMessage(LogLevel.Critical, "HomeAssistantMqtt start failed: mqttconnection is null");
            return false;
        }

        if (await MqttSendConfigurationMessages() == false)
        {
            LogMessage(LogLevel.Critical, "HomeAssistantMqtt start failed: could not send configuration MQTT messages");
            return false;
        }

        _mqttConnection.MqttMessageReceived += MqttMessageReceivedHandler;
        
        return true;
    }

    /// <summary>
    /// Handle Home Assistant MQTT messages
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="ArgumentNullException"></exception>
    private void MqttMessageReceivedHandler(object? sender, MqttMessageReceivedEventArgs args)
    {
        if (_settings == null) throw new ArgumentNullException(nameof(_settings));
        if (args.Topic == _settings.MqttTimeFeedTopic) {
        } 
        else if (args.Topic == _settings.MqttPowerFeedTopic)
        {
            MqttPowerMessage? powerMessage = null;
            try { powerMessage = JsonSerializer.Deserialize<MqttPowerMessage>(args.Payload); }
            catch (Exception ex) { LogMessage(LogLevel.Error, $"Exception decoding MQTT power message: {ex.Message}"); }
            if (powerMessage != null)
            {
                //Console.WriteLine($"Got power message from Home Assistant. Battery Level = {powerMessage.BatteryLevel}%");
            }
        }     
    }
    
    /// <summary>
    /// Send MQTT messages for Home Assistant device configurations
    /// </summary>
    /// <returns></returns>
    private async Task<bool> MqttSendConfigurationMessages()
    {
        if (_settings == null) throw new ApplicationException("HomeAssistantMqtt.MqttSendConfigurationMessages, _settings is null");
        if (_mqttConnection == null) throw new ApplicationException("HomeAssistantMqtt.MqttSendConfigurationMessages, _mqttConnection is null");
        string deviceName = _settings.MqttDeviceName;
        string deviceUniqueId = deviceName.Replace(" ","") + "-id";
        string entityName = "Battery Charge";
        string entityUniqueId = deviceName.Replace(" ","") + entityName.Replace(" ","") + "-id";
        string availabilityTopic = "homeassistant/number/" + deviceName.Replace(" ","") + "/availability";
        string availableMessage = "online";
        string unavailableMessage = "offline";
        string configTopic = $"homeassistant/number/{deviceName.Replace(" ","")}/{entityName.Replace(" ","")}/config"; 
        string stateTopic = $"homeassistant/number/{deviceName.Replace(" ","")}/{entityName.Replace(" ","")}/state";

        MqttDeviceConfigMessage configMessage = new MqttDeviceConfigMessage(
            entityName, entityUniqueId, 
            new string[] { deviceUniqueId }, deviceName, 
            availabilityTopic, availableMessage, unavailableMessage,
            "battery", stateTopic, stateTopic, "box",
            "%", 0.00, 100.00, 0.01);
        string topic = configTopic;
        string payload = JsonSerializer.Serialize(configMessage);
        string? result = await _mqttConnection.SendMessageAsync(topic, payload);
        
        if (result == null) return true;
        
        LogMessage(LogLevel.Error, "Sending configuration messages to home Assistant failed: " + result);
        return false;
    }

    
    /// <summary>
    /// Send an event indicating a log message is available
    /// </summary>
    /// <param name="level"></param>
    /// <param name="message"></param>
    private void LogMessage(LogLevel level, string message)
    {
        if (LogMessageAvailableEvent != null) LogMessageAvailableEvent(this, new LogAvailableEventArgs(level, message));
    }

}