using Shouldly;
using Microsoft.Extensions.Logging;
using Moq;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Services;

/// <summary>
/// Tests for JUnitParserService
/// Note: These are structural tests. Full integration tests require async file I/O setup.
/// </summary>
public class JUnitParserServiceTests
{
    private readonly JUnitParserService _service;
    private readonly Mock<IVersionMapperService> _mockVersionMapper;
    private readonly Mock<ILogger<JUnitParserService>> _mockLogger;

    public JUnitParserServiceTests()
    {
        _mockVersionMapper = new Mock<IVersionMapperService>();
        _mockLogger = new Mock<ILogger<JUnitParserService>>();
        _service = new JUnitParserService(_mockVersionMapper.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Assert
        _service.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_NullVersionMapper_ShouldThrow()
    {
        // Act & Assert
        var act = () => new JUnitParserService(null!, _mockLogger.Object);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act & Assert
        var act = () => new JUnitParserService(_mockVersionMapper.Object, null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ParseJUnitXmlAsync_LifecycleHookFailure_ClassifiesHookMetadata()
    {
        _mockVersionMapper.Setup(m => m.MapVersion("dev")).Returns("1.0.0");

        var tempFile = Path.Combine(Path.GetTempPath(), $"junit-hook-{Guid.NewGuid():N}.xml");
        await File.WriteAllTextAsync(tempFile, """
<testsuite name="Regression Tests for Alarm Permissions" timestamp="2026-03-15T08:00:00Z">
  <testcase name="Regression Tests for Alarm Permissions &quot;after all&quot; hook for &quot;PEXC-28074 Permissions to Sign Alarms&quot;" classname="&quot;after all&quot; hook for &quot;PEXC-28074 Permissions to Sign Alarms&quot;" time="0.000">
    <failure message="teardown failed">stack trace</failure>
  </testcase>
</testsuite>
""");

        try
        {
            var parsedPath = new ParsedFilePath
            {
                BuildNumber = 42,
                BuildId = "Release-42_123456",
                VersionRaw = "dev",
                TestType = "E2E",
                NamedConfig = "Default1",
                DomainId = "Core",
                FilePath = tempFile,
                FileName = Path.GetFileName(tempFile)
            };

            var results = await _service.ParseJUnitXmlAsync(tempFile, parsedPath);

            results.ShouldHaveSingleItem();
            var result = results[0];
            result.IsLifecycleHook.ShouldBeTrue();
            result.LifecycleHookType.ShouldBe(TestLifecycleHookType.AfterAll);
            result.LifecycleHookTarget.ShouldBe("PEXC-28074 Permissions to Sign Alarms");
            result.Status.ShouldBe(TestStatus.Fail);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    // Note: Additional tests for ParseJUnitXmlAsync would require:
    // - Creating temp files with XML content
    // - Async test methods
    // - Mocking IVersionMapperService behavior
    // These tests demonstrate the test structure and validation approach
}
