using TestResultBrowser.Web.Services;

namespace TestResultBrowser.Web.Models;

/// <summary>
/// User-configurable application settings stored in userdata database
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Unique identifier for settings (always "default")
    /// </summary>
    public string Id { get; set; } = "default";

    /// <summary>
    /// Polling interval in minutes for file watcher
    /// Default: 15 minutes
    /// </summary>
    public int PollingIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Base URL for Polarion integration
    /// </summary>
    public string? PolarionBaseUrl { get; set; } = "https://polarion.example.com";

    /// <summary>
    /// Maximum memory to use in GB
    /// </summary>
    public int MaxMemoryGB { get; set; } = 16;

    /// <summary>
    /// Flaky test detection thresholds
    /// </summary>
    public FlakyTestThresholds FlakyTestThresholds { get; set; } = new();
}
