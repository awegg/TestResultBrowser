using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using TestResultBrowser.Web.Components.Shared;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;
using MudBlazor.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components;

namespace TestResultBrowser.Tests.Components;

public class FilterPanelTests
{
    private static (TestContext ctx, Mock<ITestDataService> dataServiceMock) CreateContext()
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

        var mock = new Mock<ITestDataService>();
        mock.Setup(s => s.GetAllTestResults()).Returns(CreateSampleResults());
        ctx.Services.AddSingleton(mock.Object);

        return (ctx, mock);
    }

    [Fact]
    public void ApplyFilters_EmitsCriteria_WithDefaultsIncludingLatestBuildOnly()
    {
        var (ctx, _) = CreateContext();
        FilterCriteria? criteria = null;

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, typeof(MudPopoverProvider));
            builder.CloseComponent();
            builder.OpenComponent<FilterPanel>(1);
            builder.AddAttribute(2, nameof(FilterPanel.OnFiltersChanged), EventCallback.Factory.Create<FilterCriteria>(new object(), (Action<FilterCriteria>)(c => criteria = c)));
            builder.CloseComponent();
        });

        cut.FindAll("button").First(b => b.TextContent.Contains("Apply Filters"))
            .Click();

        criteria.ShouldNotBeNull();
        criteria!.Domains.ShouldBe(new[] { "Core", "UI" }, ignoreOrder: true);
        criteria.Features.ShouldBe(new[] { "Dashboard", "Reporting" }, ignoreOrder: true);
        criteria.Versions.ShouldBe(new[] { "1.14.0", "2.00.0" }, ignoreOrder: true);
        criteria.Configurations.ShouldBe(new[] { "1.14.0_E2E_Default1_Core", "2.00.0_E2E_Default2_UI" }, ignoreOrder: true);
        criteria.Builds.Count.ShouldBe(1);
        criteria.Builds[0].ShouldBe("Release-252_181639");
        criteria.Statuses.ShouldBe(new[] { "Pass", "Fail", "Skip" }, ignoreOrder: true);
    }

    [Fact]
    public void ClearFilters_ResetsSelections_AndInvokesCallback()
    {
        var (ctx, _) = CreateContext();
        FilterCriteria? criteria = null;

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, typeof(MudPopoverProvider));
            builder.CloseComponent();
            builder.OpenComponent<FilterPanel>(1);
            builder.AddAttribute(2, nameof(FilterPanel.OnFiltersChanged), EventCallback.Factory.Create<FilterCriteria>(new object(), (Action<FilterCriteria>)(c => criteria = c)));
            builder.CloseComponent();
        });

        cut.FindAll("button").First(b => b.TextContent.Contains("Clear All"))
            .Click();

        criteria.ShouldNotBeNull();
        criteria!.Domains.ShouldBe(new[] { "Core", "UI" }, ignoreOrder: true);
        criteria.Features.ShouldBe(new[] { "Dashboard", "Reporting" }, ignoreOrder: true);
        criteria.Versions.ShouldBe(new[] { "1.14.0", "2.00.0" }, ignoreOrder: true);
        criteria.Configurations.ShouldBe(new[] { "1.14.0_E2E_Default1_Core", "2.00.0_E2E_Default2_UI" }, ignoreOrder: true);
        criteria.Builds.Count.ShouldBe(1);
        criteria.Builds[0].ShouldBe("Release-252_181639");
        criteria.Statuses.ShouldBe(new[] { "Pass", "Fail", "Skip" }, ignoreOrder: true);
    }

    private static IEnumerable<TestResult> CreateSampleResults()
    {
        return new List<TestResult>
        {
            new()
            {
                Id = "r1",
                DomainId = "Core",
                FeatureId = "Dashboard",
                Feature = "Dashboard",
                TestSuiteId = "suite1",
                ConfigurationId = "1.14.0_E2E_Default1_Core",
                BuildId = "Release-252_181639",
                BuildNumber = 252,
                TestFullName = "A.B.C",
                ClassName = "Cls",
                MethodName = "M",
                Machine = "machine1",
                Status = TestStatus.Pass,
                ExecutionTimeSeconds = 1.0,
                Timestamp = DateTime.UtcNow
            },
            new()
            {
                Id = "r2",
                DomainId = "UI",
                FeatureId = "Reporting",
                Feature = "Reporting",
                TestSuiteId = "suite2",
                ConfigurationId = "2.00.0_E2E_Default2_UI",
                BuildId = "Release-250_181500",
                BuildNumber = 250,
                TestFullName = "D.E.F",
                ClassName = "Cls2",
                MethodName = "M2",
                Machine = "machine2",
                Status = TestStatus.Fail,
                ExecutionTimeSeconds = 2.0,
                Timestamp = DateTime.UtcNow.AddMinutes(-1)
            }
        };
    }
}
