using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Provides helper methods for extracting work item references and generating URLs.
/// </summary>
public interface IWorkItemLinkService
{
    /// <summary>
    /// Extract work item IDs from arbitrary text.
    /// </summary>
    List<string> ExtractTicketIds(string? text);

    /// <summary>
    /// Generate a link for a given work item ID using the configured base URL.
    /// Returns null when no base URL is configured.
    /// </summary>
    string? GenerateTicketUrl(string ticketId);

    /// <summary>
    /// Convert work item IDs into reference objects with URLs when available.
    /// </summary>
    List<WorkItemReference> GetTicketReferences(IEnumerable<string> ticketIds);
}
