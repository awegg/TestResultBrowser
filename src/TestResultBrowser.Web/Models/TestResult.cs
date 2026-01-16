namespace TestResultBrowser.Web.Models;

/// <summary>
/// Represents a single test case execution from JUnit XML
/// </summary>
public record TestResult
{
    /// <summary>Composite key: {ConfigId}_{BuildId}_{TestFullName}</summary>
    public required string Id { get; init; }
    
    /// <summary>Full test name, e.g., "AlarmManagerTests.TestDownloadReport"</summary>
    public required string TestFullName { get; init; }
    
    /// <summary>Class name, e.g., "AlarmManagerTests"</summary>
    public required string ClassName { get; init; }
    
    /// <summary>Method name, e.g., "TestDownloadReport"</summary>
    public required string MethodName { get; init; }
    
    /// <summary>Test execution status (Pass/Fail/Skip)</summary>
    public required TestStatus Status { get; init; }
    
    /// <summary>Test execution duration in seconds</summary>
    public required double ExecutionTimeSeconds { get; init; }
    
    /// <summary>Timestamp when test was executed</summary>
    public required DateTime Timestamp { get; init; }
    
    /// <summary>Error message if test failed (null if passed)</summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>Stack trace if test failed (null if passed)</summary>
    public string? StackTrace { get; init; }
    
    // Associated metadata
    
    /// <summary>Domain ID, e.g., "Core", "TnT_Prod"</summary>
    public required string DomainId { get; init; }
    
    /// <summary>Feature ID, e.g., "AlarmManager"</summary>
    public required string FeatureId { get; init; }
    
    /// <summary>Test suite ID from JUnit XML testsuite name</summary>
    public required string TestSuiteId { get; init; }
    
    /// <summary>Configuration ID: {Version}_{TestType}_{NamedConfig}_{Domain}</summary>
    public required string ConfigurationId { get; init; }
    
    /// <summary>Build ID, e.g., "Release-252_181639"</summary>
    public required string BuildId { get; init; }
    
    /// <summary>Build number extracted from BuildId, e.g., 252</summary>
    public required int BuildNumber { get; init; }
    
    /// <summary>Machine hostname (if available in XML)</summary>
    public required string Machine { get; init; }
    
    /// <summary>Extracted Polarion ticket references, e.g., ["PEXC-28044"]</summary>
    public List<string> PolarionTickets { get; init; } = new();
}
