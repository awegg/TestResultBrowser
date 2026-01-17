namespace TestResultBrowser.Web.Models;

/// <summary>
/// Cell in the configuration matrix representing one configuration's summary
/// </summary>
public record ConfigCell
{
    public required string ConfigurationId { get; init; }
    public required double PassRate { get; init; }
    public required int TotalTests { get; init; }
    public required int PassedTests { get; init; }
    public required int FailedTests { get; init; }
    public bool IsFailing => FailedTests > 0;
}
