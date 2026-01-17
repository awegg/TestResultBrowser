namespace TestResultBrowser.Web.Models;

/// <summary>
/// Release triage result containing configuration matrix and summaries
/// </summary>
public record ReleaseTriageResult
{
    public required string ReleaseBuildId { get; init; }
    public string? PreviousReleaseBuildId { get; init; }

    public required ConfigurationMatrix Matrix { get; init; }
    public required List<string> FailingConfigurations { get; init; }
    public required Dictionary<string, double> DomainPassRates { get; init; }
    public required Dictionary<string, double> FeaturePassRates { get; init; }
    public ComparisonMetrics? ComparisonToPrevious { get; init; }
}
