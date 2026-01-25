using Shouldly;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Services;

public class SettingsServiceTests
{
    private static IOptions<TestResultBrowserOptions> MakeOptions(string userDataPath)
    {
        return Options.Create(new TestResultBrowserOptions
        {
            UserDataPath = userDataPath,
            PollingIntervalMinutes = 7,
            WorkItemBaseUrl = "https://workitems.local",
            MaxMemoryGB = 32,
            FlakyTestThresholds = new FlakyTestThresholds
            {
                RollingWindowSize = 14,
                TriggerPercentage = 25,
                ClearAfterConsecutivePasses = 3
            }
        });
    }

    [Fact]
    public void GetSettings_FirstRun_ShouldCreateDefaultsFromOptions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var logger = new Mock<ILogger<SettingsService>>();
        var svc = new SettingsService(MakeOptions(tempDir), logger.Object);

        try
        {
            var settings = svc.GetSettings();
            settings.PollingIntervalMinutes.ShouldBe(7);
            settings.WorkItemBaseUrl.ShouldBe("https://workitems.local");
            settings.MaxMemoryGB.ShouldBe(32);
            settings.FlakyTestThresholds.RollingWindowSize.ShouldBe(14);
            Directory.Exists(tempDir).ShouldBeTrue();
            File.Exists(Path.Combine(tempDir, "settings.db")).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SaveSettings_ShouldPersistAndRaiseEvent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var logger = new Mock<ILogger<SettingsService>>();
        var svc = new SettingsService(MakeOptions(tempDir), logger.Object);

        int eventCount = 0;
        svc.SettingsChanged += (_, __) => eventCount++;

        try
        {
            var settings = svc.GetSettings();
            settings.PollingIntervalMinutes = 9;
            await svc.SaveSettingsAsync(settings);

            var loaded = svc.GetSettings();
            loaded.PollingIntervalMinutes.ShouldBe(9);
            eventCount.ShouldBe(1);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ResetToDefaults_ShouldRestoreOptionValues()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var logger = new Mock<ILogger<SettingsService>>();
        var svc = new SettingsService(MakeOptions(tempDir), logger.Object);

        try
        {
            var settings = svc.GetSettings();
            settings.PollingIntervalMinutes = 22;
            await svc.SaveSettingsAsync(settings);

            await svc.ResetToDefaultsAsync();
            var restored = svc.GetSettings();
            restored.PollingIntervalMinutes.ShouldBe(7);
            restored.MaxMemoryGB.ShouldBe(32);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SaveSettings_PersistsAcrossServiceInstances()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var logger = new Mock<ILogger<SettingsService>>();

        try
        {
            // Service 1: Create and save settings
            var svc1 = new SettingsService(MakeOptions(tempDir), logger.Object);
            var settings = svc1.GetSettings();
            settings.PollingIntervalMinutes = 12;  // Change value
            await svc1.SaveSettingsAsync(settings);

            // Verify it was saved in service 1
            var reloaded1 = svc1.GetSettings();
            reloaded1.PollingIntervalMinutes.ShouldBe(12, "Service 1 should see the saved value");

            // Service 2: Create fresh instance and verify it reads the persisted value
            var svc2 = new SettingsService(MakeOptions(tempDir), logger.Object);
            var loaded = svc2.GetSettings();
            loaded.PollingIntervalMinutes.ShouldBe(12, "Service 2 should read the persisted value from disk");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}