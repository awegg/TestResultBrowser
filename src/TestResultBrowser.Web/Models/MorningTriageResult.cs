namespace TestResultBrowser.Web.Models;

/// <summary>
/// Morning triage analysis result comparing today vs yesterday
/// </summary>
public record MorningTriageResult
{
    /// <summary>Build ID for today's run</summary>
    public required string TodayBuildId { get; init; }

    /// <summary>Build ID for yesterday's run</summary>
    public required string YesterdayBuildId { get; init; }

    /// <summary>List of tests that passed yesterday but failed today</summary>
    public required List<TriageNewFailure> NewFailures { get; init; }

    /// <summary>List of tests that failed yesterday but passed today</summary>
    public required List<TriageFixedTest> FixedTests { get; init; }

    /// <summary>List of tests that are still failing from yesterday</summary>
    public required List<TestResult> StillFailing { get; init; }

    /// <summary>Today's overall pass rate</summary>
    public required double TodayPassRate { get; init; }

    /// <summary>Yesterday's overall pass rate</summary>
    public required double YesterdayPassRate { get; init; }

    /// <summary>Total count of tests in today's run</summary>
    public required int TotalTestsToday { get; init; }

    /// <summary>Total count of tests in yesterday's run</summary>
    public required int TotalTestsYesterday { get; init; }

    /// <summary>Configurations expected for the overnight run window</summary>
    public required List<string> ExpectedConfigurations { get; init; }

    /// <summary>Configurations missing from today's run</summary>
    public required List<string> MissingConfigurations { get; init; }

    /// <summary>Whether the current run appears complete enough for normal triage</summary>
    public required bool IsRunComplete { get; init; }

    /// <summary>Human-readable completeness summary for the overnight banner</summary>
    public required string CompletenessMessage { get; init; }
}
