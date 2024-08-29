namespace EnergySuper;

public class Settings
{
    public string MqttBroker { get; set; } = "default";
    public int MqttPort { get; set; } = 1883;
    public string MqttUsername { get; set; } = string.Empty;
    public string MqttPassword { get; set; } = string.Empty;
    public string AmberUrl { get; set; } = string.Empty;
    public string AmberToken { get; set; } = string.Empty;
    public string AmberSiteId { get; set; } = string.Empty;
    public string PowerWallLocalUrl { get; set; } = string.Empty;
    public string PowerWallLocalEmail { get; set; } = string.Empty;
    public string PowerWallLocalPassword { get; set; } = string.Empty;
    public int AmberApiReadFrequencyInSeconds { get; set; } = 60;
    public int Pw2LocalApiReadFrequencyInSeconds { get; set; } = 60;

    public string? Load()
    {
        // Build a config object, using env vars and JSON providers.
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Get values from the config given their key and their target type.
        try
        {
            string? cs = config.GetValue<string>("Settings:MqttBroker");
            if (!string.IsNullOrWhiteSpace(cs)) MqttBroker = cs;
            
            MqttPort = config.GetValue<int>("Settings:MqttPort");

            cs = config.GetValue<string>("Settings:MqttUsername");
            if (!string.IsNullOrWhiteSpace(cs)) MqttUsername = cs;

            cs = config.GetValue<string>("Settings:MqttPassword");
            if (!string.IsNullOrWhiteSpace(cs)) MqttPassword = cs;
            
            cs = config.GetValue<string>("Settings:AmberUrl");
            if (!string.IsNullOrWhiteSpace(cs)) AmberUrl = cs;

            cs = config.GetValue<string>("Settings:AmberToken");
            if (!string.IsNullOrWhiteSpace(cs)) AmberToken = cs;

            cs = config.GetValue<string>("Settings:AmberSiteId");
            if (!string.IsNullOrWhiteSpace(cs)) AmberSiteId = cs;

            cs = config.GetValue<string>("Settings:PowerWallLocalUrl");
            if (!string.IsNullOrWhiteSpace(cs)) PowerWallLocalUrl = cs;

            cs = config.GetValue<string>("Settings:PowerWallLocalEmail");
            if (!string.IsNullOrWhiteSpace(cs)) PowerWallLocalEmail = cs;

            cs = config.GetValue<string>("Settings:PowerWallLocalPassword");
            if (!string.IsNullOrWhiteSpace(cs)) PowerWallLocalPassword = cs;

            AmberApiReadFrequencyInSeconds = config.GetValue<int>("Settings:AmberApiReadFrequencyInSeconds");
            
            Pw2LocalApiReadFrequencyInSeconds = config.GetValue<int>("Settings:Pw2LocalApiReadFrequencyInSeconds");
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return null;
    }
}