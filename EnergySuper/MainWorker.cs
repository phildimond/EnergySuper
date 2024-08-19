namespace EnergySuper;

public class MainWorker : BackgroundService
{
    private readonly ILogger<MainWorker>? _logger;
    private readonly Settings _settings = new Settings();
    private MqttConnection _mqttConnection;

    public MainWorker(ILogger<MainWorker>? logger)//, MQTTConnection mqttConnection)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await LogMessage(LogLevel.Information,"The EnergySuper Main Engine started executing at: {time}", DateTimeOffset.Now);
        
        Thread.Sleep(1000);
        
        // Load the settings. Bomb on failure
        string? slr = _settings.Load();
        if (slr != null)
        {
            await LogMessage(LogLevel.Critical, "Failed to load settings: " + slr);
            Environment.Exit(-1);
        }

Console.WriteLine("Broker Address: " + _settings.MqttBroker);
Console.WriteLine("Broker Port: " + _settings.MqttPort);
Console.WriteLine("Broker Username: " + _settings.MqttUsername);
Console.WriteLine("Broker Password: " + _settings.MqttPassword);
            
        // Connect to the MQTT Broker. Log and exit on failure.
        _mqttConnection = new MqttConnection(_settings.MqttBroker, _settings.MqttPort, _settings.MqttUsername, _settings.MqttPassword);
        try { _mqttConnection.Connect(); }
        catch (Exception ex)
        {
            await LogMessage(LogLevel.Critical, "Failed to connect to mqtt: " + ex.Message);
            Environment.Exit(-1);
        }

        // Main application loop
        while (!stoppingToken.IsCancellationRequested)
        {
            await LogMessage(LogLevel.Information,"The EnergySuper Main Worker was still running at: {time}", DateTimeOffset.Now);
            Thread.Sleep(1000);
        }

        try { _mqttConnection.Disconnect(); }
        catch (Exception ex) { await LogMessage(LogLevel.Critical, "Failed to disconnect from mqtt: " + ex.Message); }
        
        await LogMessage(LogLevel.Information,"The EnergySuper Main Engine stopped executing at: {time}", DateTimeOffset.Now);

    }

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