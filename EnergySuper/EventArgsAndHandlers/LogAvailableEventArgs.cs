namespace EnergySuper.EventArgsAndHandlers;

public delegate void LogAvailableEventHandler(object? sender, LogAvailableEventArgs e); 

public class LogAvailableEventArgs(LogLevel logLevel, string message)
{
    public LogLevel Level { get; set; } = logLevel;
    public string Message { get; set; } = message;
}