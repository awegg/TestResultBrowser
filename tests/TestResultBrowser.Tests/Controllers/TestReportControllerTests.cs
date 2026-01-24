using Shouldly;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TestResultBrowser.Web.Controllers;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Controllers;

public class TestReportControllerTests
{
    private readonly Mock<IOptions<TestResultBrowserOptions>> _mockOptions;
    private readonly Mock<ILogger<TestReportController>> _mockLogger;
    private readonly TestReportController _controller;
    private readonly string _testBaseDirectory;

    public TestReportControllerTests()
    {
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), "TestReportControllerTests");
        Directory.CreateDirectory(_testBaseDirectory);

        _mockLogger = new Mock<ILogger<TestReportController>>();
        _mockOptions = new Mock<IOptions<TestResultBrowserOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(new TestResultBrowserOptions
        {
            FileSharePath = _testBaseDirectory
        });

        _controller = new TestReportController(_mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task GetReport_ValidPath_ShouldReturnContent()
    {
        // Arrange
        var reportDir = Path.Combine(_testBaseDirectory, "test-report");
        Directory.CreateDirectory(reportDir);
        var indexFile = Path.Combine(reportDir, "index.html");
        var htmlContent = @"<!DOCTYPE html><html><body>Test Report</body></html>";
        await File.WriteAllTextAsync(indexFile, htmlContent);

        try
        {
            var relativePath = "test-report";

            // Act
            var result = await _controller.GetReport(relativePath);

            // Assert
            result.ShouldBeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult.ShouldNotBeNull();
            contentResult.ContentType.ShouldBe("text/html");
            contentResult.Content.ShouldNotBeNull();
            contentResult.Content.ShouldContain("Test Report");
        }
        finally
        {
            if (Directory.Exists(reportDir))
                Directory.Delete(reportDir, true);
        }
    }

    [Theory]
    [InlineData(@"..\..\..\windows\system32\config\sam")]
    [InlineData(@"..\..\sensitive.txt")]
    [InlineData(@"..\outside.txt")]
    public async Task GetReport_PathTraversal_ShouldReturnBadRequest(string attackPath)
    {
        // Act
        var result = await _controller.GetReport(attackPath);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest.ShouldNotBeNull();
        var message = badRequest.Value?.ToString();
        message.ShouldNotBeNull();
        message!.ShouldContain("Invalid file path");
    }

    [Fact]
    public async Task GetReport_AbsolutePathOutsideBase_ShouldReturnBadRequest()
    {
        // Arrange
        var outsidePath = @"C:\Windows\System32\config\sam";

        // Act
        var result = await _controller.GetReport(outsidePath);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetReport_NonExistentFile_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentPath = "nonexistent.xml";

        // Act
        var result = await _controller.GetReport(nonExistentPath);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetReport_NullOrEmptyPath_ShouldReturnBadRequest(string? invalidPath)
    {
        // Act
        var result = await _controller.GetReport(invalidPath!);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        // Act
        var mockLogger = new Mock<ILogger<TestReportController>>();
        Action act = () => new TestReportController(mockLogger.Object, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }
}
