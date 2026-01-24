using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Services;

public class PolarionLinkServiceTests
{
	private readonly Mock<ILogger<PolarionLinkService>> _logger = new();

	private PolarionLinkService CreateService(string baseUrl)
	{
		var options = Options.Create(new TestResultBrowserOptions
		{
			PolarionBaseUrl = baseUrl
		});

		return new PolarionLinkService(options, _logger.Object);
	}

	[Fact]
	public void ExtractTicketIds_FindsDistinctIds()
	{
		var service = CreateService("https://polarion.example.com/");

		var ids = service.ExtractTicketIds("PEXC-12345 and also PEXC-12345 plus PEXC-999");

		ids.ShouldBe(new[] { "PEXC-12345", "PEXC-999" }, ignoreOrder: true);
	}

	[Theory]
	[InlineData("https://polarion.example.com/", "PEXC-28044", "https://polarion.example.com/PEXC-28044")]
	[InlineData("https://polarion.example.com/#/project/PEXC/workitem?id=", "PEXC-28044", "https://polarion.example.com/#/project/PEXC/workitem?id=PEXC-28044")]
	public void GenerateTicketUrl_AppendsTicketId(string baseUrl, string ticket, string expected)
	{
		var service = CreateService(baseUrl);

		var url = service.GenerateTicketUrl(ticket);

		url.ShouldBe(expected);
	}

	[Fact]
	public void GetTicketReferences_ProducesUrlsWhenConfigured()
	{
		var service = CreateService("https://polarion.example.com/");

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
}
