namespace TestResultBrowser.Web.Models;

/// <summary>
/// Comparison metrics between two builds for release triage
/// </summary>
public record ComparisonMetrics
{
    /// <summary>Tests that regressed (passed previously, failed now)</summary>
    public required int TestsRegressed { get; init; }

    /// <summary>Tests that improved (failed previously, passed now)</summary>
    public required int TestsImproved { get; init; }

    /// <summary>Overall pass rate change in percentage points</summary>
    public required double PassRateChange { get; init; }
}
