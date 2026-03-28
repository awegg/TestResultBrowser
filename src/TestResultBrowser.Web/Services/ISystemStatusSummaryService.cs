using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

public interface ISystemStatusSummaryService
{
    Task<SystemStatusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    void Invalidate();
}