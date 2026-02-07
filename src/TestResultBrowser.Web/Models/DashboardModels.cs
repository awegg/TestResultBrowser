namespace TestResultBrowser.Web.Models;

/// <summary>
/// Config matrix cell with historical trend data for sparklines
/// </summary>
public record MatrixCell(
    string ConfigId,
    double PassRate,
    int Total,
    int Passed,
    int Failed,
    List<double?> Trend,
    List<string> BuildIds,
    List<DateTime> Timestamps);

/// <summary>
/// Version x NamedConfig matrix for dashboard display
/// </summary>
public record DashboardMatrix(
    List<string> Versions,
    List<string> NamedConfigs,
    Dictionary<string, Dictionary<string, MatrixCell>> Cells);

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
