namespace TestResultBrowser.Web.Services;

public static class ConfigurationLabelHelper
{
    public static string GetConfigurationLabel(string configId)
    {
        if (string.Equals(configId, ConfigurationHistoryService.AllConfigurationsId, StringComparison.OrdinalIgnoreCase))
        {
            return "All configurations";
        }

        if (string.IsNullOrWhiteSpace(configId))
        {
            return configId;
        }

        var parts = configId.Split('_', 4);
        if (parts.Length == 0)
        {
            return configId;
        }

        parts[0] = parts[0] switch
        {
            "Development" => "dev",
            _ => TryFormatPxrel(parts[0])
        };

        return string.Join("_", parts);
    }

    private static string TryFormatPxrel(string versionPart)
    {
        var segments = versionPart.Split('.');
        if (segments.Length == 3
            && int.TryParse(segments[0], out var major)
            && int.TryParse(segments[1], out var minor)
            && int.TryParse(segments[2], out var patch)
            && patch == 0)
        {
            return $"PXrel{(major * 100) + minor}";
        }

        return versionPart;
    }
}
