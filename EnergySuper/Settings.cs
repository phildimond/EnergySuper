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
            
            cs = config.GetValue<string>("Settings:MqttPort");
            if (!string.IsNullOrWhiteSpace(cs)) MqttPort = Convert.ToInt32(cs);

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
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return null;
    }
}