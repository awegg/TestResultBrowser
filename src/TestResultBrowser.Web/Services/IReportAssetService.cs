using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Resolves screenshot and video assets from report.json for a test.
/// </summary>
public interface IReportAssetService
{
    Task<ReportAssetInfo?> GetAssetsAsync(TestResult testResult);
}
