using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using TestResultBrowser.Web.Common;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Default implementation for Polarion link generation and ticket extraction.
/// </summary>
public class PolarionLinkService : IPolarionLinkService
{
    private readonly ILogger<PolarionLinkService> _logger;
    private readonly string _baseUrl;
    private static readonly Regex TicketRegex = new(TestResultConstants.RegexPatterns.PolarionTicketId, RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public PolarionLinkService(IOptions<TestResultBrowserOptions> options, ILogger<PolarionLinkService> logger)
    {
        _logger = logger;
        _baseUrl = options.Value.PolarionBaseUrl?.Trim() ?? string.Empty;
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

        try
        {
            // Preserve callers' formatting of base URL; just append the ticket id
            return _baseUrl.EndsWith('/') || _baseUrl.EndsWith('=')
                ? _baseUrl + ticketId
                : _baseUrl + "/" + ticketId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate Polarion URL for ticket {TicketId}", ticketId);
            return null;
        }
    }

    public List<PolarionTicketReference> GetTicketReferences(IEnumerable<string> ticketIds)
    {
        var references = new List<PolarionTicketReference>();
        if (ticketIds == null)
        {
            return references;
        }

        foreach (var id in ticketIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            references.Add(new PolarionTicketReference
            {
                TicketId = id,
                Url = GenerateTicketUrl(id)
            });
        }

        return references;
    }
}
