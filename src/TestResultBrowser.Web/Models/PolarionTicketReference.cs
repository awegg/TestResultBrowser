namespace TestResultBrowser.Web.Models;

/// <summary>
/// Represents a Polarion ticket reference extracted from test metadata.
/// </summary>
public record PolarionTicketReference
{
    /// <summary>Ticket identifier, e.g., PEXC-28044.</summary>
    public required string TicketId { get; init; }

    /// <summary>Fully-qualified link to the Polarion work item (null if base URL not configured).</summary>
    public string? Url { get; init; }
}
