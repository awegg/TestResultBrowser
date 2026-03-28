using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

public interface ISystemStatusSummaryService
{
    /// <summary>
— Retrieves the current system status snapshot.
— </summary>
— <param name="cancellationToken">Token to cancel the operation.</param>
— <returns>The current SystemStatusSnapshot.</returns>
Task<SystemStatusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
/// Marks any previously obtained system status snapshot as invalid so a fresh snapshot will be produced on the next request.
/// </summary>
void Invalidate();
}