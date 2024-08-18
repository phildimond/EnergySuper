namespace EnergySuper;

/// <summary>
/// Abstract so the compiler doesn't bitch that "Program is never instantiated".
/// </summary>
public abstract class Program
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