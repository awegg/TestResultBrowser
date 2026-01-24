namespace TestResultBrowser.Web.Models;

/// <summary>
/// Test suite from JUnit XML (corresponds to &lt;testsuite&gt; element)
/// </summary>
public record TestSuite
{
    /// <summary>Test suite ID from JUnit XML testsuite name attribute</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name</summary>
    public required string DisplayName { get; init; }

    /// <summary>Parent feature ID</summary>
    public required string FeatureId { get; init; }

    /// <summary>Parent domain ID</summary>
    public required string DomainId { get; init; }

    /// <summary>List of test case full names in this suite</summary>
    public List<string> TestNames { get; init; } = new();
}
