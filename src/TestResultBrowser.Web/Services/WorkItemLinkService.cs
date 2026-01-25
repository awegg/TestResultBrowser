using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TestResultBrowser.Web.Common;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Default implementation for work item link generation and ID extraction.
/// </summary>
public class WorkItemLinkService : IWorkItemLinkService
{
    private readonly ILogger<WorkItemLinkService> _logger;
    private readonly string _baseUrl;
    private static readonly Regex TicketRegex = new(TestResultConstants.RegexPatterns.WorkItemId, RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public WorkItemLinkService(IOptions<TestResultBrowserOptions> options, ILogger<WorkItemLinkService> logger)
    {
        _logger = logger;
        _baseUrl = options.Value.WorkItemBaseUrl?.Trim() ?? string.Empty;
    }

    public List<string> ExtractTicketIds(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        var matches = TicketRegex.Matches(text);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            ids.Add(match.Value);
        }

        return ids.ToList();
    }

    public string? GenerateTicketUrl(string ticketId)
    {
        if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(_baseUrl))
        {
            return null;
        }

        // Preserve callers' formatting of base URL; just append the ticket id
        return _baseUrl.EndsWith('/') || _baseUrl.EndsWith('=')
            ? _baseUrl + ticketId
            : _baseUrl + "/" + ticketId;
    }

    public List<WorkItemReference> GetTicketReferences(IEnumerable<string> ticketIds)
    {
        var references = new List<WorkItemReference>();
        if (ticketIds == null)
        {
            return references;
        }

        foreach (var id in ticketIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            references.Add(new WorkItemReference
            {
                TicketId = id,
                Url = GenerateTicketUrl(id)
            });
        }

        return references;
    }
}
