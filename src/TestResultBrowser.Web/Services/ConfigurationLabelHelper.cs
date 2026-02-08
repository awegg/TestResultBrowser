namespace TestResultBrowser.Web.Services;

public static class ConfigurationLabelHelper
{
    public static string GetConfigurationLabel(string configId)
    {
        return string.Equals(configId, ConfigurationHistoryService.AllConfigurationsId, StringComparison.OrdinalIgnoreCase)
            ? "All configurations"
            : configId;
    }
}
