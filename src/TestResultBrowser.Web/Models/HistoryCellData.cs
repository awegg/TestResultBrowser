namespace TestResultBrowser.Web.Models;

/// <summary>
/// Pass/fail data for a node in a specific build column
/// </summary>
public class HistoryCellData
{
    /// <summary>
    /// Number of passed tests in this build
    /// </summary>
    public int Passed { get; set; }

    /// <summary>
    /// Number of failed tests in this build
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Number of skipped tests in this build
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Total tests in this build
    /// </summary>
    public int Total => Passed + Failed + Skipped;

    /// <summary>
    /// Cell status for coloring (Green = all pass, Red = has failures, Gray = no tests)
    /// </summary>
    public HistoryCellStatus Status
    {
        get
        {
            if (Total == 0) return HistoryCellStatus.NoData;
            if (Failed > 0) return HistoryCellStatus.HasFailures;
            if (Skipped > 0 && Passed == 0) return HistoryCellStatus.AllSkipped;
            return HistoryCellStatus.AllPassed;
        }
    }

    /// <summary>
    /// Display text (e.g., "36/36", "35/36")
    /// </summary>
    public string DisplayText => Total > 0 ? $"{Passed}/{Total}" : "-";

    /// <summary>
    /// Percentage passed in this build (0-100)
    /// </summary>
    public double PassPercentage => Total > 0 ? (Passed * 100.0) / Total : 0;

    /// <summary>
    /// Directory path to test report (for test nodes only)
    /// </summary>
    public string? ReportDirectoryPath { get; set; }
}

/// <summary>
/// Status of a history cell for color coding
/// </summary>
public enum HistoryCellStatus
{
    AllPassed,      // Green - all tests passed
    HasFailures,    // Red - some tests failed
    AllSkipped,     // Gray - all tests skipped
    NoData          // Gray - no test data
}
