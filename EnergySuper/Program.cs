namespace EnergySuper;

/// <summary>
/// Main program entry point
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService(
            serviceProvider => new MainWorker(
                serviceProvider.GetService<ILogger<MainWorker>>()
            )
        );
        builder.Services.AddSystemd();
        var host = builder.Build();
        host.Run();
    }
}