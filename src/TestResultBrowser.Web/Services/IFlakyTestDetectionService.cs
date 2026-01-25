using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Service for detecting and analyzing flaky tests (tests that fail inconsistently)
/// </summary>
public interface IFlakyTestDetectionService
{
    /// <summary>
    /// Detect flaky tests from a collection of test results
    /// </summary>
    /// <param name="allResults">All test results to analyze</param>
    /// <param name="failureRateThreshold">Threshold to consider test flaky (0.0-1.0, default 0.2 = 20%)</param>
    /// <param name="recentRunWindow">Number of recent runs to analyze (default 20)</param>
    /// <returns>List of flaky test reports sorted by failure rate (highest first)</returns>
    List<FlakyTestReport> DetectFlakyTests(
        IEnumerable<TestResult> allResults,
        double failureRateThreshold = 0.20,
        int recentRunWindow = 20);

    /// <summary>
    /// Get flaky tests filtered by additional criteria
    /// </summary>
    List<FlakyTestReport> FilterFlakyTests(
        List<FlakyTestReport> reports,
        string? configurationFilter = null,
        DateRange? dateRange = null,
        TrendDirection? trendFilter = null);
}

/// <summary>
/// Date range filter
/// </summary>
public record DateRange(DateTime StartDate, DateTime EndDate);
