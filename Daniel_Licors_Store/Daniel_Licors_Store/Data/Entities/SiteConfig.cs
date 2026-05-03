namespace Daniel_Licors_Store.Data.Entities;

public class SiteConfig
{
    public int SiteConfigId { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
}