using AmberElectricityAPI;
using AmberElectricityAPI.Models;
using EnergySuper.EventArgsAndHandlers;
using PowerWallLocalApi;

namespace EnergySuper;

public class MainWorker : BackgroundService
{
    private readonly ILogger<MainWorker>? _logger;
    private readonly Settings _settings = new Settings();
    private MqttConnection? _mqttConnection;
    private AmberElectricity _amberElectricity;
    private AmberData _amberData = new AmberData();
    private DateTime _lastAmberPollTime = DateTime.MinValue;
    private PowerWall2Local _localPowerWall2;
    private DateTime _lastPowerWall2LocalPollTime = DateTime.MinValue;
    
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
        /*
        Console.WriteLine($"Amber URL = {_settings.AmberUrl}");
        Console.WriteLine($"Amber Token = {_settings.AmberToken}");
        Console.WriteLine($"Amber SiteID = {_settings.AmberSiteId}");
        */
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

        // Subscribe to a topic
        string topic = "homeassistant/CurrentTime";
        Exception? exr = await _mqttConnection.Subscribe(topic);
        if (exr is not null)
            await LogMessage(LogLevel.Error, $"Failed to subscribe to topic{topic} - Error {exr.Message}");

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
        //Console.Write($"Received message: {args.Topic}: {args.Payload}");
        //Console.WriteLine("\t... press Control-C to exit the program.");
        switch (args.Topic)
        {
            case "homeassistant/CurrentTime":
                if (_mqttConnection == null) throw new ApplicationException("MQTT connection is null in MqttMessageReceivedHandler");
                //var result = await _mqttConnection.SendMessageAsync("homeassistant/my_code_response",
                //    $"Hello, MQTT! I got the time as " + args.Payload);
                //if (result != null) Console.WriteLine($"Sending MQTT message failed. Reason: {result}");
                
                // If we're due to update Amber data, do that....
                if (DateTime.Now - _lastAmberPollTime > TimeSpan.FromSeconds(_settings.AmberApiReadFrequencyInSeconds))
                {
                    _lastAmberPollTime = DateTime.Now;
                    try { UpdateAmberInfo(_amberElectricity); }
                    catch (Exception ex) { await LogMessage(LogLevel.Error, $"Amber update failed: {ex.Message}"); }
                }
                
                // If we're due to update PowerWall local data, do that....
                if (DateTime.Now - _lastPowerWall2LocalPollTime > TimeSpan.FromSeconds(_settings.Pw2LocalApiReadFrequencyInSeconds))
                {
                    _lastPowerWall2LocalPollTime = DateTime.Now;
                    try { UpdatePowerWallLocal(_localPowerWall2); }
                    catch (Exception ex) { await LogMessage(LogLevel.Error, $"PowerWall2 Local API update failed: {ex.Message}"); }
                }
                break;
            default: await LogMessage(LogLevel.Error, $"MQTT message received from unexpected topic [{args.Topic}]");
                break;
        }
    }

    /// <summary>
    /// Call the Amber API to update our information about Amber pricing
    /// </summary>
    /// <param name="amberElectricity"></param>
    /// <exception cref="ApplicationException"></exception>
    private async void UpdateAmberInfo(AmberElectricity amberElectricity)
    {
        var prices = await amberElectricity.GetSitePricesAsync(DateTime.Today, DateTime.Today.AddDays(1));
        //var prices = await _amberElectricity.GetCurrentPricesAsync(0, 0);
        if (prices.recs == null) 
            throw new ApplicationException($"Unable to retrieve Amber Site Prices: Http response was {prices.httpStatusCode}");

        Console.Write($"{DateTime.Now:HH:mm:ss} Amber Electricity prices: ");
        foreach (var rec in prices.recs)
        {
            if (rec.IntervalType == IntervalTypeEnum.CurrentInterval)
                Console.Write($"{rec.ChannelType} = {rec.PerKwh}c/kWh ... ");
        }
        Console.WriteLine();
        
        /*
        var usage = amberElectricity.GetSiteUsage(DateTime.Today.AddDays(-1), DateTime.Today);
        if (usage.recs == null) 
            throw new ApplicationException($"Unable to retrieve Amber Site Prices: Http response was {prices.httpStatusCode}");
        
        Console.WriteLine($"\n{DateTime.Now:HH:mm:ss} Amber Electricity usage: ");
        foreach (var rec in usage.recs)
        {
            Console.WriteLine($"{rec.Date:dd/MM/yy} {rec.StartTime.ToLocalTime():HH:mm} - {rec.ChannelType} = {rec.Kwh} at {rec.PerKwh}c/kWh = {(rec.Cost/100):C}");
        }
        Console.WriteLine();
        */
    }

    /// <summary>
    /// Update data from the PowerWall battery local API
    /// </summary>
    /// <param name="powerWallLocal"></param>
    private void UpdatePowerWallLocal(PowerWall2Local powerWallLocal)
    {
        if (!_localPowerWall2.Login().Success)
            throw new ApplicationException($"Error attempting to login to PowerWall2 Local API.");
        var localPwMeters = powerWallLocal.AggregateMeters();
        if (localPwMeters == null)
            throw new ApplicationException($"Unable to retrieve PowerWall2 Local API data");//: Http response was {prices.httpStatusCode}");
        var result = _localPowerWall2.Logout();
        if (!result.success)
            throw new ApplicationException($"Error attempting to log out of PowerWall2 Local API. Http response was {result.httpStatusCode}");
        Console.WriteLine($"PowerWall 2 Battery power now = {(localPwMeters.Battery.InstantPower/1000):0.000}kW");
        Console.WriteLine($"PowerWall 2 Site power now = {(localPwMeters.Site.InstantPower/1000):0.000}kW");
        Console.WriteLine($"PowerWall 2 Load power now = {(localPwMeters.Load.InstantPower/1000):0.000}kW");
        Console.WriteLine($"PowerWall 2 Solar power now = {(localPwMeters.Solar.InstantPower/1000):0.000}kW");
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