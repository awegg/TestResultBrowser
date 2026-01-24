namespace TestResultBrowser.Web.Models;

/// <summary>
/// Metadata about a configuration including last update time
/// </summary>
public class ConfigurationMetadata
{
    /// <summary>Configuration ID</summary>
    public required string Id { get; init; }

    /// <summary>Last update datetime for this configuration</summary>
    public DateTime LastUpdateTime { get; init; }

    /// <summary>Formatted display string for last update (German format: DD.MM.YYYY HH:mm)</summary>
    public string LastUpdateDisplay => LastUpdateTime.ToString("dd.MM.yyyy HH:mm");
}
