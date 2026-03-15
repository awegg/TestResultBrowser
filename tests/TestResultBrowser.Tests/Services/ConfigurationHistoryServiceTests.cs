using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Services;

public class ConfigurationHistoryServiceTests
{
    [Fact]
    public async Task GetConfigurationHistoryAsync_SeparatesHookFailuresFromRealTests()
    {
        var testDataService = new TestDataService();
        var workItemLinkService = new Mock<IWorkItemLinkService>();
        workItemLinkService.Setup(s => s.GetTicketReferences(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<WorkItemReference>());

        var logger = new Mock<ILogger<ConfigurationHistoryService>>();
        var service = new ConfigurationHistoryService(testDataService, workItemLinkService.Object, logger.Object);

        const string configurationId = "1.0.0_E2E_Default1_Core";
        const string buildId = "Release-42_123456";
        var timestamp = new DateTime(2026, 3, 15, 8, 0, 0, DateTimeKind.Utc);

        testDataService.AddOrUpdateTestResults(new[]
        {
            CreateResult(
                id: "test-pass",
                fullName: "AlarmPermissions.PEXC-28074 Permissions to Sign Alarms",
                className: "AlarmPermissions",
                methodName: "PEXC-28074 Permissions to Sign Alarms",
                status: TestStatus.Pass,
                configurationId: configurationId,
                buildId: buildId,
                buildNumber: 42,
                timestamp: timestamp,
                suiteId: "Core_Regression Tests for Alarm Permissions"),
            CreateResult(
                id: "hook-after-all",
                fullName: "\"after all\" hook for \"PEXC-28074 Permissions to Sign Alarms\".Regression Tests for Alarm Permissions \"after all\" hook for \"PEXC-28074 Permissions to Sign Alarms\"",
                className: "\"after all\" hook for \"PEXC-28074 Permissions to Sign Alarms\"",
                methodName: "Regression Tests for Alarm Permissions \"after all\" hook for \"PEXC-28074 Permissions to Sign Alarms\"",
                status: TestStatus.Fail,
                configurationId: configurationId,
                buildId: buildId,
                buildNumber: 42,
                timestamp: timestamp.AddSeconds(1),
                suiteId: "Core_Regression Tests for Alarm Permissions",
                hookType: TestLifecycleHookType.AfterAll,
                hookTarget: "PEXC-28074 Permissions to Sign Alarms",
                errorMessage: "teardown failed")
        });

        var result = await service.GetConfigurationHistoryAsync(configurationId, 1);

        result.TotalTests.ShouldBe(1);
        result.PassedTests.ShouldBe(1);
        result.FailedTests.ShouldBe(0);

        var rootNode = result.HierarchyNodes.ShouldHaveSingleItem();
        var featureNode = rootNode.Children.ShouldHaveSingleItem();
        var suiteNode = featureNode.Children.ShouldHaveSingleItem();

        suiteNode.HistoryCells.ShouldHaveSingleItem();
        suiteNode.HistoryCells[0].DisplayText.ShouldBe("1/1");
        suiteNode.HistoryCells[0].HookFailures.ShouldBe(1);

        suiteNode.Children.Count.ShouldBe(2);
        suiteNode.Children.Count(n => n.NodeType == HierarchyNodeType.Hook).ShouldBe(1);
        suiteNode.Children.Count(n => n.NodeType == HierarchyNodeType.Test).ShouldBe(1);

        var hookNode = suiteNode.Children.Single(n => n.NodeType == HierarchyNodeType.Hook);
        hookNode.Name.ShouldContain("Teardown failure");
        hookNode.LifecycleHookType.ShouldBe(TestLifecycleHookType.AfterAll);
        hookNode.LifecycleHookTarget.ShouldBe("PEXC-28074 Permissions to Sign Alarms");
        hookNode.HistoryCells[0].Status.ShouldBe(HistoryCellStatus.HasFailures);
    }

    private static TestResult CreateResult(
        string id,
        string fullName,
        string className,
        string methodName,
        TestStatus status,
        string configurationId,
        string buildId,
        int buildNumber,
        DateTime timestamp,
        string suiteId,
        TestLifecycleHookType hookType = TestLifecycleHookType.None,
        string? hookTarget = null,
        string? errorMessage = null)
    {
        return new TestResult
        {
            Id = id,
            TestFullName = fullName,
            ClassName = className,
            MethodName = methodName,
            LifecycleHookType = hookType,
            LifecycleHookTarget = hookTarget,
            Status = status,
            ExecutionTimeSeconds = 1.0,
            Timestamp = timestamp,
            ErrorMessage = errorMessage,
            StackTrace = errorMessage,
            DomainId = "Core",
            FeatureId = "Core_AlarmPermissions",
            TestSuiteId = suiteId,
            ConfigurationId = configurationId,
            BuildId = buildId,
            BuildNumber = buildNumber,
            Machine = "host",
            Feature = "Px Core - Alarm Permissions",
            WorkItemIds = new List<string>(),
            ReportDirectoryPath = null
        };
    }
}