using Shouldly;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TestResultBrowser.Web.Hubs;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Services;

public class FileWatcherServiceTests
{
    private static IOptions<TestResultBrowserOptions> MakeOptions(string path)
    {
        return Options.Create(new TestResultBrowserOptions
        {
            FileSharePath = path,
            PollingIntervalMinutes = 1
        });
    }

    [Fact]
    public async Task ScanNowAsync_EmptyDirectory_ShouldProcessZeroFiles()
    {
        // Arrange temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var logger = new Mock<ILogger<FileWatcherService>>();
        var hub = new Mock<IHubContext<TestDataHub>>();
        var settingsSvc = new Mock<ISettingsService>();
        settingsSvc.Setup(s => s.GetSettings()).Returns(new TestResultBrowser.Web.Models.ApplicationSettings
        {
            Id = "default",
            PollingIntervalMinutes = 1,
            MaxMemoryGB = 16,
            WorkItemBaseUrl = "",
            FlakyTestThresholds = new TestResultBrowser.Web.Models.FlakyTestThresholds()
        });

        var services = new ServiceCollection();
        services.AddSingleton<IFilePathParserService>(new FilePathParserService());
        services.AddSingleton<IJUnitParserService>(new JUnitParserService(new VersionMapperService(), new Mock<ILogger<JUnitParserService>>().Object));
        services.AddSingleton<ITestDataService>(new TestDataService());
        var provider = services.BuildServiceProvider();

        var svc = new FileWatcherService(
            logger.Object,
            provider,
            MakeOptions(tempDir),
            hub.Object,
            settingsSvc.Object);

        try
        {
            // Act
            await svc.ScanNowAsync();

            // Assert
            svc.LastScanFileCount.ShouldBe(0);
            svc.LastScanTime.ShouldNotBeNull();
            svc.IsScanningInProgress.ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ScanNowAsync_WithXmlFiles_ShouldNotThrow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var xmlPath = Path.Combine(tempDir, "sample.xml");
        await File.WriteAllTextAsync(xmlPath, "<?xml version=\"1.0\"?><testsuite name=\"suite\"><testcase classname=\"C\" name=\"M\" time=\"0.1\" /></testsuite>");

        var logger = new Mock<ILogger<FileWatcherService>>();
        var hub = new Mock<IHubContext<TestDataHub>>();
        var settingsSvc = new Mock<ISettingsService>();
        settingsSvc.Setup(s => s.GetSettings()).Returns(new TestResultBrowser.Web.Models.ApplicationSettings
        {
            Id = "default",
            PollingIntervalMinutes = 1,
            MaxMemoryGB = 16,
            WorkItemBaseUrl = "",
            FlakyTestThresholds = new TestResultBrowser.Web.Models.FlakyTestThresholds()
        });

        var services = new ServiceCollection();
        services.AddSingleton<IFilePathParserService>(new FilePathParserService());
        services.AddSingleton<IJUnitParserService>(new JUnitParserService(new VersionMapperService(), new Mock<ILogger<JUnitParserService>>().Object));
        services.AddSingleton<ITestDataService>(new TestDataService());
        var provider = services.BuildServiceProvider();

        var svc = new FileWatcherService(
            logger.Object,
            provider,
            MakeOptions(tempDir),
            hub.Object,
            settingsSvc.Object);

        try
        {
            await svc.ScanNowAsync();
            svc.LastScanFileCount.ShouldBeGreaterThan(0);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
