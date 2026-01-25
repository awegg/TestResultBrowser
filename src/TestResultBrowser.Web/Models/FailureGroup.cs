namespace TestResultBrowser.Web.Models;

public record FailureGroup
(
    string GroupKey,
    string RepresentativeMessage,
    int TestCount,
    IReadOnlyList<string> DomainIds,
    IReadOnlyList<string> FeatureIds,
    IReadOnlyList<TestResult> TestResults
)
{
    public double SimilarityScore { get; init; } = 1.0; // 1.0 for exact, <1.0 for fuzzy
}
