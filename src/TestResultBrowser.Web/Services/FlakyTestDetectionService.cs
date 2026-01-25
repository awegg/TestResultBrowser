using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Detects and analyzes flaky tests that fail inconsistently
/// </summary>
public class FlakyTestDetectionService : IFlakyTestDetectionService
{
    private readonly ILogger<FlakyTestDetectionService> _logger;

    public FlakyTestDetectionService(ILogger<FlakyTestDetectionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detect flaky tests by analyzing failure rate over a rolling window
    /// </summary>
    public List<FlakyTestReport> DetectFlakyTests(
        IEnumerable<TestResult> allResults,
        double failureRateThreshold = 0.20,
        int recentRunWindow = 20)
    {
        // Clamp threshold to valid range
        failureRateThreshold = Math.Clamp(failureRateThreshold, 0.0, 1.0);
        recentRunWindow = Math.Max(1, recentRunWindow);

        // Group by test name and get recent runs for each
        var resultsByTest = allResults
            .OrderByDescending(tr => tr.Timestamp)
            .GroupBy(tr => tr.TestFullName)
            .ToList();

        var flakyTests = new List<FlakyTestReport>();

        foreach (var testGroup in resultsByTest)
        {
            var recentRuns = testGroup.Take(recentRunWindow).OrderBy(tr => tr.Timestamp).ToList();

            if (recentRuns.Count < 2)
                continue; // Need at least 2 runs to determine flakiness

            var failureCount = recentRuns.Count(tr => tr.Status == TestStatus.Fail);
            var passCount = recentRuns.Count(tr => tr.Status == TestStatus.Pass);
            var skipCount = recentRuns.Count(tr => tr.Status == TestStatus.Skip);
            
            // Calculate failure rate only against pass/fail runs (exclude skipped)
            var totalRelevantRuns = failureCount + passCount;
            if (totalRelevantRuns == 0)
                continue; // Skip if no pass/fail runs
            
            var failureRate = (double)failureCount / totalRelevantRuns;

            // Only flag tests that meet the flakiness threshold
            if (failureRate < failureRateThreshold || failureRate >= 1.0)
                continue; // Skip tests that are too stable or always failing

            var lastFailure = recentRuns.Where(tr => tr.Status == TestStatus.Fail).OrderByDescending(tr => tr.Timestamp).FirstOrDefault();
            var lastPass = recentRuns.Where(tr => tr.Status == TestStatus.Pass).OrderByDescending(tr => tr.Timestamp).FirstOrDefault();
            var lastStatus = recentRuns.Last().Status;

            // Calculate trend (comparing first half vs second half of runs)
            var trend = CalculateTrend(recentRuns);

            var report = new FlakyTestReport(
                TestFullName: testGroup.Key,
                FailureRate: failureRate,
                TotalRuns: totalRelevantRuns,  // Count only pass/fail runs
                FailureCount: failureCount,
                PassCount: passCount,
                LastStatus: lastStatus,
                LastFailure: lastFailure?.Timestamp ?? DateTime.MinValue,
                LastPass: lastPass?.Timestamp ?? DateTime.MinValue,
                Trend: trend,
                RecentRuns: (IReadOnlyList<TestResult>)recentRuns
            );
            
            _logger.LogInformation(
                "Detected flaky test: {TestName}, FailureRate={FailureRate:P0}, TotalRuns={TotalRuns}, Skipped={SkipCount}, Trend={Trend}",
                testGroup.Key, failureRate, totalRelevantRuns, skipCount, trend);

            flakyTests.Add(report);
        }

        // Sort by failure rate (highest first) then by most recent failure
        return flakyTests
            .OrderByDescending(f => f.FailureRate)
            .ThenByDescending(f => f.LastFailure)
            .ToList();
    }

    /// <summary>
    /// Filter flaky tests by additional criteria
    /// </summary>
    public List<FlakyTestReport> FilterFlakyTests(
        List<FlakyTestReport> reports,
        string? configurationFilter = null,
        DateRange? dateRange = null,
        TrendDirection? trendFilter = null)
    {
        var filtered = reports.AsEnumerable();

        // Filter by configuration
        if (!string.IsNullOrEmpty(configurationFilter))
        {
            filtered = filtered.Where(f => f.RecentRuns.Any(tr => tr.ConfigurationId == configurationFilter));
        }

        // Filter by date range
        if (dateRange != null)
        {
            filtered = filtered.Where(f => 
                f.RecentRuns.Any(tr => tr.Timestamp >= dateRange.StartDate && tr.Timestamp <= dateRange.EndDate));
        }

        // Filter by trend
        if (trendFilter.HasValue)
        {
            filtered = filtered.Where(f => f.Trend == trendFilter.Value);
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Calculate trend direction based on failure rate in first vs second half of runs
    /// </summary>
    private static TrendDirection CalculateTrend(List<TestResult> recentRuns)
    {
        if (recentRuns.Count < 2)
            return TrendDirection.Stable;

        var midpoint = recentRuns.Count / 2;
        var firstHalf = recentRuns.Take(midpoint).ToList();
        var secondHalf = recentRuns.Skip(midpoint).ToList();

        if (firstHalf.Count == 0 || secondHalf.Count == 0)
            return TrendDirection.Stable;

        var firstHalfFailureRate = (double)firstHalf.Count(tr => tr.Status == TestStatus.Fail) / firstHalf.Count;
        var secondHalfFailureRate = (double)secondHalf.Count(tr => tr.Status == TestStatus.Fail) / secondHalf.Count;

        const double threshold = 0.05; // 5% difference threshold

        if (secondHalfFailureRate < firstHalfFailureRate - threshold)
            return TrendDirection.Improving;
        if (secondHalfFailureRate > firstHalfFailureRate + threshold)
            return TrendDirection.Worsening;

        return TrendDirection.Stable;
    }
}
