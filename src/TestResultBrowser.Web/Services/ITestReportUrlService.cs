namespace TestResultBrowser.Web.Services;

/// <summary>
/// Builds web URLs for static test report directories.
/// </summary>
public interface ITestReportUrlService
{
    /// <summary>
    /// Returns a URL to open the report directory, or null if it cannot be mapped safely.
    /// </summary>
    string? GetReportUrl(string? reportDirectoryPath);
}
