namespace EnergySuper;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<MainWorker>();
        builder.Services.AddSystemd();
        var host = builder.Build();
        host.Run();
    }
}