namespace TestResultBrowser.Web.Models;

/// <summary>
/// Represents a test that failed yesterday but passed today (Morning Triage)
/// </summary>
public record TriageFixedTest
{
    /// <summary>Full class.method name of the test</summary>
    public required string TestFullName { get; init; }
    
    /// <summary>Domain ID where this test resides</summary>
    public required string DomainId { get; init; }
    
    /// <summary>Feature ID where this test resides</summary>
    public required string FeatureId { get; init; }
    
    /// <summary>List of configuration IDs where this test was fixed</summary>
    public required List<string> FixedInConfigs { get; init; }
    
    /// <summary>Timestamp when the test passed</summary>
    public required DateTime PassedOn { get; init; }
}
