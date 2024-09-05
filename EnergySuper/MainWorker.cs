using System.Text.Json;
using AmberElectricityAPI;
using AmberElectricityAPI.Models;
using EnergySuper.EventArgsAndHandlers;
using EnergySuper.Models;
using PowerWallLocalApi;

namespace EnergySuper;

public class MainWorker : BackgroundService
{
    private readonly ILogger<MainWorker>? _logger;
    private readonly Settings _settings = new Settings();
    private MqttConnection? _mqttConnection;
    private AmberElectricity? _amberElectricity;
    private DateTime _lastAmberPollTime = DateTime.MinValue;
    private PowerWall2Local? _localPowerWall2;
    private DateTime _lastPowerWall2LocalPollTime = DateTime.MinValue;
    private CurrentData _currentData = new CurrentData();
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger"></param>
    public MainWorker(ILogger<MainWorker>? logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Main worker execution method
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <exception cref="ApplicationException"></exception>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await LogMessage(LogLevel.Information, "The EnergySuper Main Engine started executing at: {time}",
            DateTimeOffset.Now);

        Thread.Sleep(1000);

        // Load the settings. Bomb on failure
        string? slr = _settings.Load();
        if (slr != null)
        {
            await LogMessage(LogLevel.Critical, "Failed to load settings: " + slr);
            Environment.Exit(-1);
        }

        // Verify the Amber Electricity connection
        try
        {
            _amberElectricity = new AmberElectricity(_settings.AmberToken, _settings.AmberSiteId);
            var prices = await _amberElectricity.GetCurrentPricesAsync(0, 0);
            if (prices.records == null) throw new ApplicationException($"Unable to retrieve API data: Http response was {prices.httpStatusCode}");
        }
        catch (Exception ex)
        {
            await LogMessage(LogLevel.Critical, "Failed to connect to Amber: " + ex.Message);
            Environment.Exit(-1);
        }
        
        // Connect to the PowerWall local API
        try
        {
            _localPowerWall2 = 
                new PowerWall2Local(_settings.PowerWallLocalUrl, _settings.PowerWallLocalEmail, _settings.PowerWallLocalPassword);
            var lir = _localPowerWall2.Login(); 
            if (lir.Success) Console.WriteLine("Successfully logged in to PowerWall local API.");
            else Console.WriteLine($"FAILED attempting to login to PowerWall local API. Http response was {lir.ResponseCode}");
            var lor = _localPowerWall2.Logout();
            if (lor.success) Console.WriteLine("Successfully logged out of PowerWall local API.");
            else Console.WriteLine($"FAILED attempting to log out of PowerWall local API. Http response was {lor.httpStatusCode}");
        }
        catch (Exception ex)
        {
            await LogMessage(LogLevel.Critical, "Failed to connect to the PowerWall 2 Local API: " + ex.Message);
            Environment.Exit(-1);
        }

        // Connect to the MQTT Broker. Log and exit on failure.
        _mqttConnection = new MqttConnection(_settings.MqttBroker, _settings.MqttPort, _settings.MqttUsername, _settings.MqttPassword);
        _mqttConnection.MqttMessageReceived += MqttMessageReceivedHandler;
        try
        {
            var result = await _mqttConnection.Connect();
            if (result != null) 
                throw new ApplicationException(result);
        }
        catch (Exception ex)
        {
            await LogMessage(LogLevel.Critical, "Failed to connect to mqtt: " + ex.Message);
            Environment.Exit(-1);
        }

        // Send configuration messages
        await MqttSendConfigurationMessages();

        // Subscribe to topics
        Exception? exr = await _mqttConnection.Subscribe(_settings.MqttTimeFeedTopic);
        if (exr != null) await LogMessage(LogLevel.Error, $"Failed to subscribe to topic{_settings.MqttTimeFeedTopic} - Error {exr.Message}");
        exr = await _mqttConnection.Subscribe(_settings.MqttPowerFeedTopic);
        if (exr != null) await LogMessage(LogLevel.Error, $"Failed to subscribe to topic{_settings.MqttPowerFeedTopic} - Error {exr.Message}");

        // Main application loop
        while (!stoppingToken.IsCancellationRequested)
        {
            //await LogMessage(LogLevel.Information,"The EnergySuper Main Worker was still running at: {time}", DateTimeOffset.Now);
            Thread.Sleep(1000);
        }

        try
        {
            _mqttConnection.Disconnect();
        }
        catch (Exception ex)
        {
            await LogMessage(LogLevel.Critical, "Failed to disconnect from mqtt: " + ex.Message);
        }

        await LogMessage(LogLevel.Information, "The EnergySuper Main Engine stopped executing at: {time}",
            DateTimeOffset.Now);

    }

    /// <summary>
    /// Process received MQTT messages 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void MqttMessageReceivedHandler(object? sender, MqttMessageReceivedEventArgs args)
    {
        if (args.Topic == _settings.MqttTimeFeedTopic) {
            if (_mqttConnection == null) throw new ApplicationException("MQTT connection is null in MqttMessageReceivedHandler");
                // If we're due to update Amber data, do that....
                if (DateTime.Now - _lastAmberPollTime > TimeSpan.FromSeconds(_settings.AmberApiReadFrequencyInSeconds))
                {
                    _lastAmberPollTime = DateTime.Now;
                    if (_amberElectricity != null) {
                        try { await UpdateAmberInfo(_amberElectricity); }
                        catch (Exception ex) { await LogMessage(LogLevel.Error, $"Amber update failed: {ex.Message}"); }
                    }
                }
                
                // If we're due to update PowerWall local data, do that....
                if (DateTime.Now - _lastPowerWall2LocalPollTime > TimeSpan.FromSeconds(_settings.Pw2LocalApiReadFrequencyInSeconds))
                {
                    _lastPowerWall2LocalPollTime = DateTime.Now;
                    if (_localPowerWall2 != null) {
                        try { await UpdatePowerWallLocal(_localPowerWall2); }
                        catch (Exception ex) { await LogMessage(LogLevel.Error, $"PowerWall2 Local API update failed: {ex.Message}"); }
                    }
                }
        } 
        else if (args.Topic == _settings.MqttPowerFeedTopic)
        {
            MqttPowerMessage? powerMessage = null;
            try { powerMessage = JsonSerializer.Deserialize<MqttPowerMessage>(args.Payload); }
            catch (Exception ex) { await LogMessage(LogLevel.Error, $"Exception decoding MQTT power message: {ex.Message}"); }

            if (powerMessage != null)
            {
                Console.WriteLine($"Got power message from Home Assistant. Battery Level = {powerMessage.BatteryLevel}%");
            }
        } 
        else  await LogMessage(LogLevel.Error, $"MQTT message received from unexpected topic [{args.Topic}]");
    }

    /// <summary>
    /// Send MQTT messages for Home Assistant device configurations
    /// </summary>
    /// <returns></returns>
    private async Task<bool> MqttSendConfigurationMessages()
    {
        if (_mqttConnection == null) return false;
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
        await LogMessage(LogLevel.Error, "Sending configuration messages to home Assistant failed: " + result);
        return false;
    }
    
    /// <summary>
    /// Call the Amber API to update our information about Amber pricing
    /// </summary>
    /// <param name="amberElectricity"></param>
    /// <exception cref="ApplicationException"></exception>
    private async Task<bool> UpdateAmberInfo(AmberElectricity amberElectricity)
    {
        if (_amberElectricity == null)
        {
            await LogMessage(LogLevel.Error, "Amber Electricity variable is null");
            return false;
        }
        var prices = await amberElectricity.GetCurrentPricesAsync(1,0);
        if (prices.records == null) 
            throw new ApplicationException($"Unable to retrieve Amber Site Prices: Http response was {prices.httpStatusCode}");

        // Process the prices
        foreach (var rec in prices.records)
        {
            switch (rec.ChannelType)
            {
                case ChannelTypeEnum.general:
                    if (rec.IntervalType == IntervalTypeEnum.CurrentInterval)
                        _currentData.CurrentPowerPriceBuy = rec.PerKwh;
                    if (rec.IntervalType == IntervalTypeEnum.ForecastInterval)
                        _currentData.ForecastPowerPriceBuy = rec.PerKwh;
                    break;
                case ChannelTypeEnum.feedIn:
                    if (rec.IntervalType == IntervalTypeEnum.CurrentInterval)
                        _currentData.CurrentPowerPriceSell = rec.PerKwh;
                    if (rec.IntervalType == IntervalTypeEnum.ForecastInterval)
                        _currentData.ForecastPowerPriceSell = rec.PerKwh;
                    break;
                case ChannelTypeEnum.controlledLoad:
                    if (rec.IntervalType == IntervalTypeEnum.CurrentInterval)
                        _currentData.CurrentPowerPriceControlledLoad = rec.PerKwh;
                    if (rec.IntervalType == IntervalTypeEnum.ForecastInterval)
                        _currentData.ForecastPowerPriceControlledLoad = rec.PerKwh;
                    break;
                case ChannelTypeEnum.unknown:
                    await LogMessage(LogLevel.Error,
                        $"Received a price for the 'Unknown' Channel Type from Amber.");
                    break;
                default:
                    await LogMessage(LogLevel.Error, $"Unrecognised Channel Type from Amber: {rec.ChannelType}");
                    break;
            }
        }
        _currentData.LastPriceUpdate = DateTime.Now;
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} Amber Electricity current prices:  Buy = {_currentData.CurrentPowerPriceBuy:0.000}c/kWh, " +
                          $"Sell = {_currentData.CurrentPowerPriceSell:0.000}c/kWh, Controlled Load = {_currentData.CurrentPowerPriceControlledLoad:0.000}c/kWh");
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} Amber Electricity forecast prices: Buy = {_currentData.ForecastPowerPriceBuy:0.000}c/kWh, " +
                          $"Sell = {_currentData.ForecastPowerPriceSell:0.000}c/kWh, Controlled Load = {_currentData.ForecastPowerPriceControlledLoad:0.000}c/kWh");
        if (_amberElectricity.RateApiCallsRemainingThisWindow != null && _amberElectricity.RateSecsToWindowReset != null 
                                                                      && _amberElectricity.RateMaxCallsPerWindow != null
                                                                      && _amberElectricity.RateSecsPerWindow != null)
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} Amber Electricity API rate info:   {_amberElectricity.RateApiCallsRemainingThisWindow} " +
                              $"of {_amberElectricity.RateMaxCallsPerWindow} calls remaining in the next {_amberElectricity.RateSecsToWindowReset} seconds " +
                              $"of the {_amberElectricity.RateSecsPerWindow} second window.");
        return true;
    }

    /// <summary>
    /// Update data from the PowerWall battery local API
    /// </summary>
    /// <param name="powerWallLocal"></param>
    private async Task<bool> UpdatePowerWallLocal(PowerWall2Local powerWallLocal)
    {
        if (_localPowerWall2 == null)
        {
            await LogMessage(LogLevel.Error, "Local PowerWall2 variable is null.");
            return false;
        }
        
        // Login to PowerWall local API, get data & log out
        if (!_localPowerWall2.Login().Success)
            throw new ApplicationException($"Error attempting to login to PowerWall2 Local API.");
        var localPwMeters = powerWallLocal.AggregateMeters();
        if (localPwMeters == null)
            throw new ApplicationException($"Unable to retrieve PowerWall2 Local API data");
        var charge = powerWallLocal.GetStateOfEnergy();
        if (charge == null)
            throw new ApplicationException($"Unable to retrieve PowerWall2 Local API data");
        var result = _localPowerWall2.Logout();
        if (!result.success)
            throw new ApplicationException($"Error attempting to log out of PowerWall2 Local API. Http response was {result.httpStatusCode}");
        
        // Calculate battery charge time if we have good previous data and we're actually charging
        TimeSpan timeToCharge = new TimeSpan(0);
        if (charge.Percentage < 99.99 && localPwMeters.Battery.InstantPower < 0 && DateTime.Now - _currentData.LastPowerUpdate < TimeSpan.FromMinutes(5))
        {
            double chargeChange = charge.Percentage - _currentData.BatteryChargePercent;
            double secsChange = (DateTime.Now - _currentData.LastPowerUpdate).TotalSeconds;
            double chargePercentPerSec = chargeChange / secsChange;
            double chargeRemaining = 100 - charge.Percentage;
            double secsToCharge = chargeRemaining / chargePercentPerSec;
            timeToCharge = TimeSpan.FromSeconds(secsToCharge); 
        }
        
        // Update values
        _currentData.BatteryPowerKw = localPwMeters.Battery.InstantPower / 1000;
        _currentData.BatteryChargePercent = charge.Percentage;
        _currentData.GridPowerKw = localPwMeters.Site.InstantPower / 1000;
        _currentData.SolarPowerKw = localPwMeters.Solar.InstantPower / 1000;
        _currentData.LoadPowerKw = localPwMeters.Load.InstantPower / 1000; 
        _currentData.LastPowerUpdate = DateTime.Now;
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} PowerWall: House = {_currentData.LoadPowerKw:0.000}kW, Solar = {_currentData.SolarPowerKw:0.000}kW, " +
                          $"Grid = {_currentData.GridPowerKw:0.000}kW, Battery = {_currentData.BatteryPowerKw:0.000}kW, " +
                          $"Charge = {charge.Percentage:0.000}%");
        if (timeToCharge.TotalSeconds != 0 && localPwMeters.Battery.InstantPower < 0.250 && charge.Percentage < 99.99)
            Console.WriteLine($"Battery should be charged in {timeToCharge.TotalSeconds} seconds by {(DateTime.Now + timeToCharge):HH:mm:ss}");
        
        // Send MQTT messages
        if (_mqttConnection != null)
        {
            string deviceName = _settings.MqttDeviceName;
            string thisEntityName = "Battery Charge";
            string stateTopic = $"homeassistant/number/{deviceName.Replace(" ", "")}/{thisEntityName.Replace(" ", "")}/state";
            string availabilityTopic = "homeassistant/number/" + deviceName.Replace(" ", "") + "/availability";
            string availableMessage = "online";

            await _mqttConnection.SendMessageAsync(availabilityTopic, availableMessage);
            await _mqttConnection.SendMessageAsync(stateTopic, charge.Percentage.ToString("0.00"));
        }

        return true;
    }
    
    /// <summary>
    /// Manage message logging to systemd
    /// </summary>
    /// <param name="logLevel"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private async Task LogMessage(LogLevel logLevel, string message, params object?[] args)
    {
        if (_logger == null || !_logger.IsEnabled(logLevel)) return;
        switch (logLevel)
        {
            case LogLevel.Information:
                await Task.Run(() => _logger.LogInformation(message, args));
                break;
            case LogLevel.Warning:
                await Task.Run(() => _logger.LogWarning(message, args));
                break;
            case LogLevel.Error:
                await Task.Run(() => _logger.LogError(message, args));
                break;
            case LogLevel.Critical:
                await Task.Run(() => _logger.LogCritical(message, args));
                break;
            case LogLevel.Debug:
                await Task.Run(() => _logger.LogDebug(message, args));
                break;
            case LogLevel.Trace:
                await Task.Run(() => _logger.LogTrace(message, args));
                break;
            case LogLevel.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

}