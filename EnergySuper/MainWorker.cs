namespace EnergySuper;

public class MainWorker : BackgroundService
{
    private readonly ILogger<MainWorker>? _logger;
    private readonly MQTTConnection _mqttConnection;

    public MainWorker(ILogger<MainWorker>? logger)//, MQTTConnection mqttConnection)
    {
        _logger = logger;
        _mqttConnection = new MQTTConnection("b", 1883, "u", "p");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await LogMessage(LogLevel.Information, "The EnergySuper Main Worker started at: {time}", DateTimeOffset.Now);

        if (Environment.UserInteractive)
        {
            Console.WriteLine("Running in an interactive environment!");
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await LogMessage(LogLevel.Information,"The EnergySuper Main Worker was still running at: {time}", DateTimeOffset.Now);
            Thread.Sleep(1000);
        }
        
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