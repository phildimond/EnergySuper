using System.Text.Json;
using EnergySuper.EventArgsAndHandlers;
using EnergySuper.Models;

namespace EnergySuper;

public class HomeAssistantMqtt (Settings? settings, MqttConnection? mqttConnection)
{
    private Settings? _settings = settings;
    private MqttConnection? _mqttConnection = mqttConnection;
    
    /// <summary>
    /// Raised when this object wishes to log a message
    /// </summary>
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
        string stateTopic = $"homeassistant/number/{deviceName.Replace(" ","")}/{entityName.Replace(" ","")}/state";

        // Battery Charge number
        MqttDeviceConfigMessage configMessage = new MqttDeviceConfigMessage(
            entityName, entityUniqueId, 
            new string[] { deviceUniqueId }, deviceName, 
            availabilityTopic, availableMessage, unavailableMessage,
            "battery", stateTopic, stateTopic, "box",
            "%", 0.00, 100.00, 0.01);
        string topic = $"homeassistant/number/{deviceName.Replace(" ","")}/{entityName.Replace(" ","")}/config";
        string payload = JsonSerializer.Serialize(configMessage);
        string? result = await _mqttConnection.SendMessageAsync(topic, payload);
        if (result != null) LogMessage(LogLevel.Error, "Sending configuration messages to home Assistant failed: " + result);
        
        // Grid Power Number
        entityName = "Grid Power";
        configMessage.EntityName = entityName;
        configMessage.EntityUniqueId = deviceName.Replace(" ","") + entityName.Replace(" ","") + "-id";
        configMessage.StateTopic = $"homeassistant/number/{deviceName.Replace(" ","")}/{entityName.Replace(" ","")}/state";
        configMessage.UnitOfMeasurement = "kW";
        configMessage.Min = 0;
        configMessage.Max = 1000;
        configMessage.Step = 0.001;
        topic = $"homeassistant/number/{deviceName.Replace(" ","")}/{entityName.Replace(" ","")}/config";
        payload = JsonSerializer.Serialize(configMessage);
        result = await _mqttConnection.SendMessageAsync(topic, payload);
        if (result != null) LogMessage(LogLevel.Error, "Sending configuration messages to home Assistant failed: " + result);

        // All done, return OK.
        return true;
    }

    /// <summary>
    /// Send the current data to Home Assistant
    /// </summary>
    /// <param name="currentData"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public async Task<bool> SendUpdatedValues(CurrentData currentData)
    {
        if (_settings == null) throw new ApplicationException("HomeAssistantMqtt.MqttSendConfigurationMessages, _settings is null"); 
        if (_mqttConnection != null)
        {
            string entityName = _settings.MqttDeviceName;
            string availabilityTopic = "homeassistant/number/" + entityName.Replace(" ", "") + "/availability";
            string availableMessage = "online";
            await _mqttConnection.SendMessageAsync(availabilityTopic, availableMessage);

            string thisEntityName = "Battery Charge";
            string stateTopic = $"homeassistant/number/{entityName.Replace(" ", "")}/{thisEntityName.Replace(" ", "")}/state";
            await _mqttConnection.SendMessageAsync(stateTopic, currentData.BatteryChargePercent.ToString("0.00"));

            thisEntityName = "Grid Power";
            stateTopic = $"homeassistant/number/{entityName.Replace(" ", "")}/{thisEntityName.Replace(" ", "")}/state";
            await _mqttConnection.SendMessageAsync(stateTopic, currentData.GridPowerKw.ToString("0.000"));

            return true;
        } else throw new ApplicationException("HomeAssistantMqtt.MqttSendConfigurationMessages, _mqttConnection is null");
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