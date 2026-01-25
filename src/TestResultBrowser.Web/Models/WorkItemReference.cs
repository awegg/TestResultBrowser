namespace TestResultBrowser.Web.Models;

/// <summary>
/// Represents a work item reference extracted from test metadata.
/// </summary>
public record WorkItemReference
{
    /// <summary>Ticket/work item identifier (e.g., PEXC-28044).</summary>
    public required string TicketId { get; init; }

    /// <summary>Fully-qualified link to the work item (null if base URL not configured).</summary>
    public string? Url { get; init; }
}
