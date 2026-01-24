using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Provides helper methods for extracting Polarion ticket references and generating URLs.
/// </summary>
public interface IPolarionLinkService
{
    /// <summary>
    /// Extract ticket IDs from arbitrary text.
    /// </summary>
    List<string> ExtractTicketIds(string? text);

    /// <summary>
    /// Generate a link for a given ticket ID using the configured Polarion base URL.
    /// Returns null when no base URL is configured.
    /// </summary>
    string? GenerateTicketUrl(string ticketId);

    /// <summary>
    /// Convert ticket IDs into reference objects with URLs when available.
    /// </summary>
    List<PolarionTicketReference> GetTicketReferences(IEnumerable<string> ticketIds);
}
