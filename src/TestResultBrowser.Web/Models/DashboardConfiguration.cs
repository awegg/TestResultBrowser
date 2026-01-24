namespace TestResultBrowser.Web.Models;

/// <summary>
/// Dashboard configuration for customizing user view preferences
/// </summary>
public class DashboardConfiguration
{
    /// <summary>
    /// LiteDB auto-increment primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Username this configuration belongs to
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Preferred default page on app load
    /// </summary>
    public string? DefaultPage { get; set; }

    /// <summary>
    /// Whether to auto-load data on page load
    /// </summary>
    public bool AutoLoadData { get; set; } = true;

    /// <summary>
    /// Default number of builds to display in history views
    /// </summary>
    public int DefaultBuildCount { get; set; } = 5;

    /// <summary>
    /// Default filter configuration ID to apply on startup
    /// </summary>
    public int? DefaultFilterId { get; set; }

    /// <summary>
    /// Theme preference (Light, Dark, System)
    /// </summary>
    public string Theme { get; set; } = "System";

    /// <summary>
    /// Whether to show flaky test indicators by default
    /// </summary>
    public bool ShowFlakyIndicators { get; set; } = true;

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
