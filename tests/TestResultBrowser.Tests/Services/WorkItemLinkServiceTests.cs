using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Services;

public class WorkItemLinkServiceTests
{
    private readonly Mock<ILogger<WorkItemLinkService>> _logger = new();

    private WorkItemLinkService CreateService(string baseUrl)
    {
        var options = Options.Create(new TestResultBrowserOptions
        {
            WorkItemBaseUrl = baseUrl
        });

        return new WorkItemLinkService(options, _logger.Object);
    }

    [Fact]
    public void ExtractTicketIds_FindsDistinctIds()
    {
        var service = CreateService("https://workitems.example.com/");

        var ids = service.ExtractTicketIds("PEXC-12345 and also PEXC-12345 plus PEXC-999");

        ids.ShouldBe(new[] { "PEXC-12345", "PEXC-999" }, ignoreOrder: true);
    }

    [Fact]
    public void ExtractTicketIds_NullOrEmpty_ReturnsEmpty()
    {
        var service = CreateService("https://workitems.example.com/");

        service.ExtractTicketIds(null).ShouldBeEmpty();
        service.ExtractTicketIds(string.Empty).ShouldBeEmpty();
        service.ExtractTicketIds("   ").ShouldBeEmpty();
    }

    [Fact]
    public void ExtractTicketIds_NoMatches_ReturnsEmpty()
    {
        var service = CreateService("https://workitems.example.com/");

        service.ExtractTicketIds("no tickets here").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("https://workitems.example.com/", "PEXC-28044", "https://workitems.example.com/PEXC-28044")]
    [InlineData("https://workitems.example.com/#/project/PEXC/workitem?id=", "PEXC-28044", "https://workitems.example.com/#/project/PEXC/workitem?id=PEXC-28044")]
    public void GenerateTicketUrl_AppendsTicketId(string baseUrl, string ticket, string expected)
    {
        var service = CreateService(baseUrl);

        var url = service.GenerateTicketUrl(ticket);

        url.ShouldBe(expected);
    }

    [Fact]
    public void GenerateTicketUrl_BlankTicketOrBaseUrl_ReturnsNull()
    {
        var serviceWithBase = CreateService("https://workitems.example.com/");
        serviceWithBase.GenerateTicketUrl(null!).ShouldBeNull();
        serviceWithBase.GenerateTicketUrl(" ").ShouldBeNull();

        var serviceWithoutBase = CreateService(string.Empty);
        serviceWithoutBase.GenerateTicketUrl("PEXC-1").ShouldBeNull();
    }

    [Fact]
    public void GetTicketReferences_ProducesUrlsWhenConfigured()
    {
        var service = CreateService("https://workitems.example.com/");

        var refs = service.GetTicketReferences(new[] { "PEXC-123", "PEXC-456" });

        refs.Count.ShouldBe(2);
        refs[0].Url.ShouldNotBeNull();
        refs[1].Url.ShouldNotBeNull();
    }

    [Fact]
    public void GetTicketReferences_AllowsNoBaseUrl()
    {
        var service = CreateService(string.Empty);

        var refs = service.GetTicketReferences(new[] { "PEXC-123" });

        refs.Single().Url.ShouldBeNull();
    }

    [Fact]
    public void GetTicketReferences_NullOrWhitespaceInput_ReturnsEmptyOrSkipsBlank()
    {
        var service = CreateService("https://workitems.example.com/");

        service.GetTicketReferences(null!).ShouldBeEmpty();

        var refs = service.GetTicketReferences(new[] { "PEXC-123", " ", "PEXC-123" });
        refs.Count.ShouldBe(1);
        refs.Single().TicketId.ShouldBe("PEXC-123");
    }
}
