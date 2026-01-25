using System.Linq;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using Shouldly;
using TestResultBrowser.Web.Components.Pages;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Components;

public class SettingsPageTests
{
    private static (TestContext ctx, Mock<ISettingsService> settingsMock, Mock<ISnackbar> snackbarMock) CreateContext(ApplicationSettings initialSettings, ApplicationSettings defaults)
    {
        var ctx = new TestContext();
        ctx.Services.AddMudServices();

        // Configure MudBlazor JS interop used by PopoverService
        ctx.JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        ctx.JSInterop.SetupVoid("mudPopover.connect", _ => true);
        ctx.JSInterop.SetupVoid("mudPopover.reposition", _ => true);
        ctx.JSInterop.SetupVoid("mudPopover.dispose", _ => true);
        // Configure MudBlazor input blur event
        ctx.JSInterop.SetupVoid("mudElementRef.addOnBlurEvent", _ => true);
        // Configure MudBlazor key interceptor
        ctx.JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        ctx.JSInterop.SetupVoid("mudKeyInterceptor.disconnect", _ => true);
        // Provide result for popover provider count
        ctx.JSInterop.Setup<int>("mudpopoverHelper.countProviders", _ => true).SetResult(1);

        // Navigation manager mock for navigation assertions
        ctx.Services.AddSingleton<NavigationManager, FakeNavigationManager>();

        var snackbarMock = new Mock<ISnackbar>();
        ctx.Services.AddSingleton(snackbarMock.Object);

        var settingsMock = new Mock<ISettingsService>();
        var current = Clone(initialSettings);

        settingsMock.Setup(s => s.GetSettings()).Returns(() => Clone(current));
        settingsMock.Setup(s => s.SaveSettingsAsync(It.IsAny<ApplicationSettings>()))
            .Callback<ApplicationSettings>(s => current = Clone(s))
            .Returns(Task.CompletedTask);
        settingsMock.Setup(s => s.ResetToDefaultsAsync())
            .Callback(() => current = Clone(defaults))
            .Returns(Task.CompletedTask);

        ctx.Services.AddSingleton(settingsMock.Object);

        return (ctx, settingsMock, snackbarMock);
    }

    [Fact]
    public void SaveSettings_PersistsChanges_AndNavigatesHome()
    {
        var initial = CreateSettings(polling: 15, maxMemory: 16, trigger: 25);
        var defaults = CreateSettings(polling: 5, maxMemory: 12, trigger: 20);
        var (ctx, settingsMock, _) = CreateContext(initial, defaults);

        var cut = ctx.RenderComponent<Settings>();

        var pollingInput = cut.FindAll("input[type='number']").First();
        pollingInput.Change("9");

        cut.FindAll("button").First(b => b.TextContent.Contains("Save Settings"))
            .Click();

        settingsMock.Verify(s => s.SaveSettingsAsync(It.Is<ApplicationSettings>(a => a.PollingIntervalMinutes == 9)), Times.Once);

        var nav = ctx.Services.GetRequiredService<FakeNavigationManager>();
        nav.Uri.ShouldEndWith("/");
    }

    [Fact]
    public void ResetToDefaults_RestoresConfiguredDefaults()
    {
        var initial = CreateSettings(polling: 20, maxMemory: 32, trigger: 35);
        var defaults = CreateSettings(polling: 7, maxMemory: 24, trigger: 30);
        var (ctx, _, _) = CreateContext(initial, defaults);

        var cut = ctx.RenderComponent<Settings>();

        cut.FindAll("button").First(b => b.TextContent.Contains("Reset to Defaults"))
            .Click();

        var pollingInput = cut.FindAll("input[type='number']").First();
        pollingInput.GetAttribute("value").ShouldBe("7");
    }

    [Fact]
    public void Cancel_NavigatesHome_WithoutSaving()
    {
        var initial = CreateSettings(polling: 12, maxMemory: 16, trigger: 25);
        var defaults = CreateSettings(polling: 12, maxMemory: 16, trigger: 25);
        var (ctx, settingsMock, _) = CreateContext(initial, defaults);

        var cut = ctx.RenderComponent<Settings>();

        cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"))
            .Click();

        var nav = ctx.Services.GetRequiredService<FakeNavigationManager>();
        nav.Uri.ShouldEndWith("/");
        settingsMock.Verify(s => s.SaveSettingsAsync(It.IsAny<ApplicationSettings>()), Times.Never);
    }

    private static ApplicationSettings CreateSettings(int polling, int maxMemory, int trigger)
    {
        return new ApplicationSettings
        {
            Id = "default",
            PollingIntervalMinutes = polling,
            WorkItemBaseUrl = "https://workitems.local",
            MaxMemoryGB = maxMemory,
            FlakyTestThresholds = new FlakyTestThresholds
            {
                RollingWindowSize = 14,
                TriggerPercentage = trigger,
                ClearAfterConsecutivePasses = 3
            }
        };
    }

    private static ApplicationSettings Clone(ApplicationSettings source)
    {
        return new ApplicationSettings
        {
            Id = source.Id,
            PollingIntervalMinutes = source.PollingIntervalMinutes,
            WorkItemBaseUrl = source.WorkItemBaseUrl,
            MaxMemoryGB = source.MaxMemoryGB,
            FlakyTestThresholds = new FlakyTestThresholds
            {
                RollingWindowSize = source.FlakyTestThresholds.RollingWindowSize,
                TriggerPercentage = source.FlakyTestThresholds.TriggerPercentage,
                ClearAfterConsecutivePasses = source.FlakyTestThresholds.ClearAfterConsecutivePasses
            }
        };
    }
}
