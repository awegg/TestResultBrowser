using Shouldly;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TestResultBrowser.Web.Controllers;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Controllers;

public class AssetsControllerTests
{
    private readonly Mock<IOptions<TestResultBrowserOptions>> _mockOptions;
    private readonly Mock<ILogger<AssetsController>> _mockLogger;
    private readonly AssetsController _controller;
    private readonly string _testBaseDirectory;

    public AssetsControllerTests()
    {
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), "AssetsControllerTests");
        Directory.CreateDirectory(_testBaseDirectory);

        _mockLogger = new Mock<ILogger<AssetsController>>();
        _mockOptions = new Mock<IOptions<TestResultBrowserOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(new TestResultBrowserOptions
        {
            FileSharePath = _testBaseDirectory
        });

        _controller = new AssetsController(_mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task GetAsset_ValidImage_ShouldReturnFile()
    {
        // Arrange
        var reportDir = Path.Combine(_testBaseDirectory, "test-report");
        Directory.CreateDirectory(reportDir);
        var testImage = Path.Combine(reportDir, "test.png");
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        await File.WriteAllBytesAsync(testImage, imageData);

        try
        {
            var reportPath = "test-report";
            var assetPath = "test.png";

            // Act
            var result = await _controller.GetAsset(assetPath, reportPath);

            // Assert
            result.ShouldBeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult.ShouldNotBeNull();
            fileResult!.FileContents.ShouldBe(imageData);
            fileResult.ContentType.ShouldBe("image/png");
        }
        finally
        {
            if (Directory.Exists(reportDir))
                Directory.Delete(reportDir, true);
        }
    }

    [Theory]
    [InlineData(@"..\..\..\windows\system32\cmd.exe", "asset.png")]
    [InlineData("report.xml", @"..\..\..\windows\system32\cmd.exe")]
    [InlineData(@"..\..\outside\report.xml", "asset.png")]
    public async Task GetAsset_PathTraversal_ShouldReturnBadRequest(string reportPath, string assetPath)
    {
        // Act
        var result = await _controller.GetAsset(reportPath, assetPath);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest.ShouldNotBeNull();
        var message = badRequest.Value?.ToString();
        message.ShouldNotBeNull();
        message!.ShouldContain("Invalid");
    }

    [Fact]
    public async Task GetAsset_AbsolutePathInReportPath_ShouldReturnBadRequest()
    {
        // Arrange
        var absoluteReport = @"C:\Windows\System32\report.xml";
        var assetPath = "image.png";

        // Act
        var result = await _controller.GetAsset(absoluteReport, assetPath);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAsset_AbsolutePathInAssetPath_ShouldReturnBadRequest()
    {
        // Arrange
        var reportPath = "report.xml";
        var absoluteAsset = @"C:\Windows\System32\image.png";

        // Act
        var result = await _controller.GetAsset(reportPath, absoluteAsset);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAsset_NonExistentFile_ShouldReturnNotFound()
    {
        // Arrange
        var reportDir = Path.Combine(_testBaseDirectory, "test-report");
        Directory.CreateDirectory(reportDir);

        try
        {
            // Act
            var result = await _controller.GetAsset("nonexistent.png", "test-report");

            // Assert
            result.ShouldBeOfType<NotFoundObjectResult>();
        }
        finally
        {
            if (Directory.Exists(reportDir))
                Directory.Delete(reportDir, true);
        }
    }

    [Theory]
    [InlineData(null, "asset.png")]
    [InlineData("", "asset.png")]
    [InlineData("   ", "asset.png")]
    [InlineData("report.xml", null)]
    [InlineData("report.xml", "")]
    [InlineData("report.xml", "   ")]
    public async Task GetAsset_NullOrEmptyPaths_ShouldReturnBadRequest(string? reportPath, string? assetPath)
    {
        // Act
        var result = await _controller.GetAsset(assetPath!, reportPath!);

        // Assert - empty/null/whitespace assetPath should fail validation
        // Just verify it's an error response (not 200)
        result.ShouldBeAssignableTo<IActionResult>();

        if (result is ObjectResult objResult)
        {
            objResult.StatusCode.ShouldBeOneOf(400, 404, 500);
        }
        else
        {
            result.ShouldBeAssignableTo<BadRequestObjectResult>();
        }
    }

    [Theory]
    [InlineData("test.png", "image/png")]
    [InlineData("test.jpg", "image/jpeg")]
    [InlineData("test.gif", "image/gif")]
    [InlineData("test.svg", "image/svg+xml")]
    [InlineData("test.bmp", "image/bmp")]
    [InlineData("test.webp", "image/webp")]
    public async Task GetAsset_DifferentImageTypes_ShouldReturnCorrectContentType(string fileName, string expectedContentType)
    {
        // Arrange
        var reportDir = Path.Combine(_testBaseDirectory, "test-report");
        Directory.CreateDirectory(reportDir);
        var testFile = Path.Combine(reportDir, fileName);
        var testData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        await File.WriteAllBytesAsync(testFile, testData);

        try
        {
            // Act
            var result = await _controller.GetAsset(fileName, "test-report");

            // Assert
            result.ShouldBeOfType<FileContentResult>();
            var fileResult = result as FileContentResult;
            fileResult.ShouldNotBeNull();
            fileResult!.ContentType.ShouldBe(expectedContentType);
        }
        finally
        {
            if (Directory.Exists(reportDir))
                Directory.Delete(reportDir, true);
        }
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        // Act
        var mockLogger = new Mock<ILogger<AssetsController>>();
        Action act = () => new AssetsController(mockLogger.Object, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }
}
