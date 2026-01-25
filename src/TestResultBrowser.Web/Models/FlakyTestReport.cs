namespace TestResultBrowser.Web.Models;

/// <summary>
/// Trend direction for flaky test failure rate
/// </summary>
public enum TrendDirection
{
    Improving,  // Failure rate decreasing
    Stable,     // Failure rate steady
    Worsening   // Failure rate increasing
}

/// <summary>
/// Report of a test identified as flaky (failing inconsistently)
/// </summary>
public record FlakyTestReport
(
    string TestFullName,
    double FailureRate,                       // 0.0-1.0 (e.g., 0.45 = 45%)
    int TotalRuns,
    int FailureCount,
    int PassCount,
    TestStatus LastStatus,
    DateTime LastFailure,
    DateTime LastPass,
    TrendDirection Trend,                     // Improving/Stable/Worsening
    IReadOnlyList<TestResult> RecentRuns      // Last N runs in chronological order (immutable)
)
{
    /// <summary>
    /// Window size for recent run history (last X runs to analyze)
    /// </summary>
    public static int RecentRunWindow => 20;
    
    /// <summary>
    /// Default threshold to consider a test "flaky"
    /// </summary>
    public static double FlakyThreshold => 0.20; // 20% failure rate
}
