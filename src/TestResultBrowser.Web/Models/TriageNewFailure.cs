namespace TestResultBrowser.Web.Models;

/// <summary>
/// Represents a test that passed yesterday but failed today (Morning Triage)
/// </summary>
public record TriageNewFailure
{
    /// <summary>Full class.method name of the test</summary>
    public required string TestFullName { get; init; }
    
    /// <summary>Domain ID where this test resides</summary>
    public required string DomainId { get; init; }
    
    /// <summary>Feature ID where this test resides</summary>
    public required string FeatureId { get; init; }
    
    /// <summary>List of configuration IDs where this test failed</summary>
    public required List<string> AffectedConfigs { get; init; }
    
    /// <summary>Error message from the failure</summary>
    public required string ErrorMessage { get; init; }
    
    /// <summary>Timestamp when the test failed</summary>
    public required DateTime FailedOn { get; init; }
    
    /// <summary>Optional stack trace</summary>
    public string? StackTrace { get; init; }
}
