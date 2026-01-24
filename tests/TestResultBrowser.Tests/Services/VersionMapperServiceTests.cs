using Shouldly;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Services;

public class VersionMapperServiceTests
{
    [Theory]
    [InlineData("PXrel114", "1.14.0")]
    [InlineData("PXREL114", "1.14.0")]
    [InlineData("PXrel252", "2.52.0")]
    public void MapVersion_PxrelPattern_ShouldMapToSemantic(string raw, string expected)
    {
        var svc = new VersionMapperService();
        svc.MapVersion(raw).ShouldBe(expected);
    }

    [Theory]
    [InlineData("dev", "Development")]
    [InlineData("DEV", "Development")]
    public void MapVersion_Dev_ShouldReturnDevelopment(string raw, string expected)
    {
        var svc = new VersionMapperService();
        svc.MapVersion(raw).ShouldBe(expected);
    }

    [Theory]
    [InlineData("", "Unknown")]
    [InlineData("   ", "Unknown")]
    [InlineData(null, "Unknown")]
    public void MapVersion_EmptyOrNull_ShouldReturnUnknown(string? raw, string expected)
    {
        var svc = new VersionMapperService();
        svc.MapVersion(raw!).ShouldBe(expected);
    }

    [Theory]
    [InlineData("1.14.0")]
    [InlineData("PXrel1x4")] // invalid format, passthrough
    [InlineData("Release-252_181639")] // other formats passthrough
    public void MapVersion_Unrecognized_ShouldPassthrough(string raw)
    {
        var svc = new VersionMapperService();
        svc.MapVersion(raw).ShouldBe(raw);
    }
}
