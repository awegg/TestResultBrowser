namespace TestResultBrowser.Web.Models;

/// <summary>
/// User-saved filter presets for reuse across sessions
/// </summary>
public class SavedFilterConfiguration
{
    /// <summary>
    /// LiteDB auto-increment primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User-friendly name for the filter (e.g., "Core Domain - Failed Tests Only")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Username who saved this filter
    /// </summary>
    public required string SavedBy { get; set; }

    /// <summary>
    /// When the filter was saved
    /// </summary>
    public required DateTime SavedDate { get; set; }

    /// <summary>
    /// Optional description of the filter's purpose
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Selected domains (empty list = all domains)
    /// </summary>
    public List<string> Domains { get; set; } = new();

    /// <summary>
    /// Selected features (empty list = all features)
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Selected versions (empty list = all versions)
    /// </summary>
    public List<string> Versions { get; set; } = new();

    /// <summary>
    /// Selected named configurations (empty list = all configs)
    /// </summary>
    public List<string> NamedConfigs { get; set; } = new();

    /// <summary>
    /// Selected machines (empty list = all machines)
    /// </summary>
    public List<string> Machines { get; set; } = new();

    /// <summary>
    /// Date range start (null = no limit)
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// Date range end (null = no limit)
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Filter to failures only (null = show all)
    /// </summary>
    public bool? OnlyFailures { get; set; }

    /// <summary>
    /// Hide flaky tests (null = show all)
    /// </summary>
    public bool? HideFlakyTests { get; set; }

    /// <summary>
    /// Selected configuration ID (for ConfigurationHistory page)
    /// </summary>
    public string? SelectedConfiguration { get; set; }

    /// <summary>
    /// Number of builds to display (for ConfigurationHistory page)
    /// </summary>
    public int? NumberOfBuilds { get; set; }
}
