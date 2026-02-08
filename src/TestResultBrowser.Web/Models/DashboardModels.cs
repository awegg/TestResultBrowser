namespace TestResultBrowser.Web.Models;

/// <summary>
/// Release matrix cell for a single build and configuration
/// </summary>
public record ReleaseMatrixCell(
    string ConfigId,
    string BuildId,
    DateTime Timestamp,
    double PassRate,
    int Total,
    int Passed,
    int Failed);

/// <summary>
/// Build info for release columns
/// </summary>
public record BuildInfo(
    string BuildId,
    DateTime Timestamp);

/// <summary>
/// Release x Configuration matrix for dashboard display
/// </summary>
public record DashboardMatrix(
    List<BuildInfo> Builds,
    List<string> Configurations,
    Dictionary<string, Dictionary<string, ReleaseMatrixCell>> Cells);

/// <summary>
/// A poorly performing configuration with regression metrics
/// </summary>
public record WorstConfig(
    string ConfigId,
    string Version,
    string NamedConfig,
    double PassRate,
    double Delta,
    int FailedTests,
    int TotalTests);

/// <summary>
/// Domain summary for dashboard cards
/// </summary>
public record DomainCard(
    string DomainId,
    double PassRate,
    double? Delta,
    int TotalTests,
    int FailedTests);
