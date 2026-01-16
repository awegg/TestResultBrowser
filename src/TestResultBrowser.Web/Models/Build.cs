namespace TestResultBrowser.Web.Models;

/// <summary>
/// Represents a specific build/release with test results
/// Extracted from Release-{BuildNumber} directory structure
/// </summary>
public record Build
{
    /// <summary>Build ID, e.g., "Release-252", "Release-181639"</summary>
    public required string Id { get; init; }
    
    /// <summary>Build number, e.g., 252, 181639</summary>
    public required int BuildNumber { get; init; }
    
    /// <summary>Timestamp of first test result in this build</summary>
    public required DateTime Timestamp { get; init; }
    
    /// <summary>List of configuration IDs tested in this build</summary>
    public List<string> ConfigurationIds { get; init; } = new();
    
    /// <summary>Total number of tests executed in this build</summary>
    public int TotalTests { get; init; }
    
    /// <summary>Number of passed tests in this build</summary>
    public int PassedTests { get; init; }
    
    /// <summary>Number of failed tests in this build</summary>
    public int FailedTests { get; init; }
    
    /// <summary>Number of skipped tests in this build</summary>
    public int SkippedTests { get; init; }
    
    /// <summary>Overall pass rate percentage (0-100)</summary>
    public double PassRate { get; init; }
}
