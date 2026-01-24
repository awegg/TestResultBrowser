namespace TestResultBrowser.Web.Models;

/// <summary>
/// Represents a single node in the hierarchical test tree
/// </summary>
public class HierarchyNode
{
    /// <summary>
    /// Node display name (e.g., "Px Core - Alarm Dashboard", "Regression Tests for Alarm Reports")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of node: Domain, Feature, Suite, or Test
    /// </summary>
    public HierarchyNodeType NodeType { get; set; }

    /// <summary>
    /// Unique identifier for this node within its type
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Pass/fail history for this node across all builds in HistoryColumns
    /// Index corresponds to HistoryColumn index
    /// </summary>
    public List<HistoryCellData> HistoryCells { get; set; } = new();

    /// <summary>
    /// Child nodes (empty if leaf node)
    /// </summary>
    public List<HierarchyNode> Children { get; set; } = new();

    /// <summary>
    /// Indentation level (0 = Feature, 1 = Suite, 2 = Test)
    /// </summary>
    public int IndentLevel { get; set; }

    /// <summary>
    /// Whether this node is initially expanded (true for parent nodes with failures)
    /// </summary>
    public bool IsExpanded { get; set; } = true;

    /// <summary>
    /// Summary statistics for this node in latest build
    /// </summary>
    public TestNodeStats LatestStats { get; set; } = new();

    /// <summary>
    /// Total passed + failed tests across all builds (for summary display)
    /// </summary>
    public int TotalTestsAcrossAllBuilds { get; set; }

    /// <summary>
    /// Total failures across all history builds
    /// </summary>
    public int TotalFailuresAcrossAllBuilds { get; set; }

    /// <summary>
    /// Directory path containing test report (index.html) - only for Test nodes
    /// </summary>
    public string? ReportDirectoryPath { get; set; }

    /// <summary>
    /// Full test name including class and method (for deduplication) - only for Test nodes
    /// </summary>
    public string? TestFullName { get; set; }

    /// <summary>
    /// Error message for failed test nodes (only populated for Test nodes with Status=Fail)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace for failed test nodes (only populated for Test nodes with Status=Fail)
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// First error message (for parent nodes, this is the error from the first failed child test)
    /// </summary>
    public string? FirstErrorMessage { get; set; }
}

/// <summary>
/// Type of hierarchy node
/// </summary>
public enum HierarchyNodeType
{
    Domain,
    Feature,
    Suite,
    Test
}

/// <summary>
/// Statistics for a single node in the latest build
/// </summary>
public class TestNodeStats
{
    /// <summary>
    /// Number of passed tests
    /// </summary>
    public int Passed { get; set; }

    /// <summary>
    /// Number of failed tests
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Number of skipped tests
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Total tests (Passed + Failed + Skipped)
    /// </summary>
    public int Total => Passed + Failed + Skipped;

    /// <summary>
    /// Pass rate percentage (0-100)
    /// </summary>
    public double PassRatePercentage => Total > 0 ? (Passed * 100.0) / Total : 0;

    /// <summary>
    /// Display string (e.g., "36/36" or "35/36")
    /// </summary>
    public string DisplayText => $"{Passed}/{Total}";
}
