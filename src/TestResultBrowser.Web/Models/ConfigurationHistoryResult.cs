namespace TestResultBrowser.Web.Models;

/// <summary>
/// Result of configuration history query for hierarchical test tree display
/// </summary>
public class ConfigurationHistoryResult
{
    /// <summary>
    /// Selected configuration identifier (e.g., "1.14.0_Regular_Win2019SQLServer2022_CORE")
    /// </summary>
    public string ConfigurationId { get; set; } = string.Empty;

    /// <summary>
    /// Latest build ID in the history
    /// </summary>
    public string LatestBuildId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of latest build
    /// </summary>
    public DateTime LatestBuildTime { get; set; }

    /// <summary>
    /// Total test count in latest build
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// Passed test count in latest build
    /// </summary>
    public int PassedTests { get; set; }

    /// <summary>
    /// Failed test count in latest build
    /// </summary>
    public int FailedTests { get; set; }

    /// <summary>
    /// Skipped test count in latest build
    /// </summary>
    public int SkippedTests { get; set; }

    /// <summary>
    /// Pass rate percentage (0-100)
    /// </summary>
    public double PassRatePercentage => TotalTests > 0 ? (PassedTests * 100.0) / TotalTests : 0;

    /// <summary>
    /// Columns representing historical builds (builds to display)
    /// </summary>
    public List<HistoryColumn> HistoryColumns { get; set; } = new();

    /// <summary>
    /// Root nodes of hierarchical tree (Domain level)
    /// </summary>
    public List<HierarchyNode> HierarchyNodes { get; set; } = new();
}
