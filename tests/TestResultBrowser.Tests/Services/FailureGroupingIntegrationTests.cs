using Shouldly;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;
using Microsoft.Extensions.Logging;
using TestResultBrowser.Tests.Utilities;

namespace TestResultBrowser.Tests.Services;

/// <summary>
/// Integration tests for FailureGrouping feature combining ITestDataService,
/// IFailureGroupingService, and filtering logic.
/// </summary>
public class FailureGroupingIntegrationTests
{
    private readonly TestDataService _testDataService;
    private readonly IFailureGroupingService _groupingService;
    private readonly ILogger<FailureGroupingService> _logger;

    public FailureGroupingIntegrationTests()
    {
        _testDataService = new TestDataService();
        _logger = new MockLogger<FailureGroupingService>();
        _groupingService = new FailureGroupingService(_logger);
    }

    private TestResult CreateTestResult(
        string id,
        string testName,
        string errorMessage,
        TestStatus status,
        string configId = "Config1",
        string domainId = "Domain1",
        string featureId = "Feature1",
        DateTime? timestamp = null)
    {
        return new TestResult
        {
            Id = id,
            TestFullName = testName,
            ClassName = "TestClass",
            MethodName = testName.Split('.').Last(),
            Status = status,
            ErrorMessage = errorMessage,
            ExecutionTimeSeconds = 1.5,
            Timestamp = timestamp ?? DateTime.UtcNow,
            DomainId = domainId,
            FeatureId = featureId,
            TestSuiteId = "TestSuite",
            ConfigurationId = configId,
            BuildId = "Build-100_123456",
            BuildNumber = 100,
            Machine = "TestMachine",
            Feature = featureId,
            WorkItemIds = new List<string>(),
            ReportDirectoryPath = null
        };
    }

    #region Load and Filter Tests

    [Fact]
    public void GroupFailures_AfterLoadingFromTestDataService_Works()
    {
        // Arrange - load test data
        var now = DateTime.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            var result = CreateTestResult(
                $"test_fail_{i}",
                $"Test.FailingTest_{i}",
                "Database connection timeout",
                TestStatus.Fail,
                "Config1",
                "Domain1",
                "Feature1",
                now.AddHours(-i));
            _testDataService.AddOrUpdateTestResult(result);
        }

        // Act
        var allTests = _testDataService.GetAllTestResults();
        var failed = allTests.Where(t => t.Status == TestStatus.Fail);
        var groups = _groupingService.GroupFailures(failed, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(5);
    }

    [Fact]
    public void FilterByDateRange_ThenGroup_ReturnsCorrectResults()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var dateOffset = 10; // days
        
        // Add old failures (should be filtered out)
        for (int i = 0; i < 3; i++)
        {
            _testDataService.AddOrUpdateTestResult(
                CreateTestResult(
                    $"old_fail_{i}",
                    $"Test.OldFail_{i}",
                    "Old error message",
                    TestStatus.Fail,
                    timestamp: now.AddDays(-20)));
        }

        // Add recent failures (should be included)
        for (int i = 0; i < 4; i++)
        {
            _testDataService.AddOrUpdateTestResult(
                CreateTestResult(
                    $"recent_fail_{i}",
                    $"Test.RecentFail_{i}",
                    "Recent error message",
                    TestStatus.Fail,
                    timestamp: now.AddDays(-5)));
        }

        // Act
        var allFailed = _testDataService.GetAllTestResults().Where(t => t.Status == TestStatus.Fail);
        var cutoffDate = now.AddDays(-dateOffset);
        var filtered = allFailed.Where(t => t.Timestamp >= cutoffDate);
        var groups = _groupingService.GroupFailures(filtered, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(4);
        groups[0].TestResults.All(t => t.Timestamp >= cutoffDate).ShouldBeTrue();
    }

    [Fact]
    public void FilterByConfiguration_ThenGroup_ReturnsCorrectResults()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        // Add failures in Config1
        for (int i = 0; i < 3; i++)
        {
            _testDataService.AddOrUpdateTestResult(
                CreateTestResult(
                    $"config1_fail_{i}",
                    $"Test.Config1Fail_{i}",
                    "Config1 error",
                    TestStatus.Fail,
                    "Config1",
                    timestamp: now));
        }

        // Add failures in Config2
        for (int i = 0; i < 2; i++)
        {
            _testDataService.AddOrUpdateTestResult(
                CreateTestResult(
                    $"config2_fail_{i}",
                    $"Test.Config2Fail_{i}",
                    "Config2 error",
                    TestStatus.Fail,
                    "Config2",
                    timestamp: now));
        }

        // Act
        var allFailed = _testDataService.GetAllTestResults().Where(t => t.Status == TestStatus.Fail);
        var filteredConfig1 = allFailed.Where(t => t.ConfigurationId == "Config1");
        var groupsConfig1 = _groupingService.GroupFailures(filteredConfig1, 0.8);

        var filteredConfig2 = allFailed.Where(t => t.ConfigurationId == "Config2");
        var groupsConfig2 = _groupingService.GroupFailures(filteredConfig2, 0.8);

        // Assert
        groupsConfig1.Count.ShouldBe(1);
        groupsConfig1[0].TestCount.ShouldBe(3);
        groupsConfig2.ShouldHaveSingleItem();
        groupsConfig2[0].TestCount.ShouldBe(2);
    }

    [Fact]
    public void FilterByDateAndConfig_ThenGroup_ReturnsCorrectResults()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Config1, recent failures
        for (int i = 0; i < 2; i++)
        {
            _testDataService.AddOrUpdateTestResult(
                CreateTestResult($"c1_recent_{i}", $"Test.C1Recent{i}", "Error", TestStatus.Fail,
                    "Config1", timestamp: now.AddDays(-5)));
        }

        // Config1, old failures
        for (int i = 0; i < 3; i++)
        {
            _testDataService.AddOrUpdateTestResult(
                CreateTestResult($"c1_old_{i}", $"Test.C1Old{i}", "Error", TestStatus.Fail,
                    "Config1", timestamp: now.AddDays(-20)));
        }

        // Config2, recent failures
        for (int i = 0; i < 4; i++)
        {
            _testDataService.AddOrUpdateTestResult(
                CreateTestResult($"c2_recent_{i}", $"Test.C2Recent{i}", "Error", TestStatus.Fail,
                    "Config2", timestamp: now.AddDays(-5)));
        }

        // Act
        var allFailed = _testDataService.GetAllTestResults().Where(t => t.Status == TestStatus.Fail);
        var cutoffDate = now.AddDays(-10);
        var filtered = allFailed
            .Where(t => t.Timestamp >= cutoffDate)
            .Where(t => t.ConfigurationId == "Config1");
        var groups = _groupingService.GroupFailures(filtered, 0.8);

        // Assert
        groups.Count.ShouldBe(1); // Config1 recent failures collapse into one normalized group
        groups[0].TestResults.All(t => t.ConfigurationId == "Config1").ShouldBeTrue();
        groups[0].TestResults.All(t => t.Timestamp >= cutoffDate).ShouldBeTrue();
    }

    #endregion

    #region Similarity Threshold with Real Data Tests

    [Fact]
    public void DifferentThresholds_ProduceDifferentGroupings()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var failures = new[]
        {
            CreateTestResult("t1", "Test.A", "Connection failed after 30 seconds", TestStatus.Fail, timestamp: now),
            CreateTestResult("t2", "Test.B", "Connection failed after 45 seconds", TestStatus.Fail, timestamp: now),
            CreateTestResult("t3", "Test.C", "Database unreachable", TestStatus.Fail, timestamp: now)
        };

        foreach (var f in failures)
            _testDataService.AddOrUpdateTestResult(f);

        // Act
        var allFailed = _testDataService.GetAllTestResults().Where(t => t.Status == TestStatus.Fail);
        var groupsStrict = _groupingService.GroupFailures(allFailed, 0.95);
        var groupsModerate = _groupingService.GroupFailures(allFailed, 0.80);
        var groupsPermissive = _groupingService.GroupFailures(allFailed, 0.60);

        // Assert - verify threshold behavior with specific content checks
        groupsStrict.Count.ShouldBeGreaterThanOrEqualTo(groupsModerate.Count);
        groupsModerate.Count.ShouldBeGreaterThanOrEqualTo(groupsPermissive.Count);
        
        // Verify all tests are accounted for in each grouping
        groupsStrict.Sum(g => g.TestCount).ShouldBe(3);
        groupsModerate.Sum(g => g.TestCount).ShouldBe(3);
        groupsPermissive.Sum(g => g.TestCount).ShouldBe(3);
        
        // At permissive threshold, similar "Connection failed" messages should merge
        var connectionGroup = groupsPermissive.FirstOrDefault(g => 
            g.RepresentativeMessage.Contains("Connection", StringComparison.OrdinalIgnoreCase));
        if (connectionGroup != null)
        {
            connectionGroup.TestCount.ShouldBeGreaterThanOrEqualTo(2);
        }
    }

    #endregion

    #region Mixed Status Tests

    [Fact]
    public void GroupFailures_IgnoresPassedTests()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        // Add passing tests
        for (int i = 0; i < 5; i++)
        {
            _testDataService.AddOrUpdateTestResult(
                CreateTestResult($"pass_{i}", $"Test.Pass{i}", "", TestStatus.Pass, timestamp: now));
        }

        // Add failing tests
        for (int i = 0; i < 3; i++)
        {
            _testDataService.AddOrUpdateTestResult(
                CreateTestResult($"fail_{i}", $"Test.Fail{i}", "Error", TestStatus.Fail, timestamp: now));
        }

        // Act
        var allTests = _testDataService.GetAllTestResults();
        var failed = allTests.Where(t => t.Status == TestStatus.Fail);
        var groups = _groupingService.GroupFailures(failed, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(3);
    }

    #endregion

    #region Multi-Domain Multi-Feature Tests

    [Fact]
    public void GroupFailures_AcrossDomains_CreatesCorrectGroups()
    {
        // Arrange
        var now = DateTime.UtcNow;
        
        // Domain1 failures
        for (int i = 0; i < 2; i++)
        {
            _testDataService.AddOrUpdateTestResult(
                CreateTestResult(
                    $"d1_f_{i}",
                    $"Test.Domain1Feature{i}",
                    "Database error",
                    TestStatus.Fail,
                    domainId: "Domain1",
                    featureId: "Feature1",
                    timestamp: now));
        }

        // Domain2 failures (same error message)
        for (int i = 0; i < 3; i++)
        {
            _testDataService.AddOrUpdateTestResult(
                CreateTestResult(
                    $"d2_f_{i}",
                    $"Test.Domain2Feature{i}",
                    "Database error",
                    TestStatus.Fail,
                    domainId: "Domain2",
                    featureId: "Feature2",
                    timestamp: now));
        }

        // Act
        var failed = _testDataService.GetAllTestResults().Where(t => t.Status == TestStatus.Fail);
        var groups = _groupingService.GroupFailures(failed, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(5);
        groups[0].DomainIds.ShouldContain("Domain1");
        groups[0].DomainIds.ShouldContain("Domain2");
        groups[0].FeatureIds.ShouldContain("Feature1");
        groups[0].FeatureIds.ShouldContain("Feature2");
    }

    #endregion

    #region Large Dataset Tests

    [Fact]
    public void GroupFailures_WithLargeDataset_CompletesSuccessfully()
    {
        // Arrange - create 100 failures in 5 groups
        var now = DateTime.UtcNow;
        var errorTypes = new[]
        {
            "Connection timeout error",
            "Authentication failed error",
            "File not found error",
            "Memory allocation error",
            "Network unreachable error"
        };

        for (int group = 0; group < 5; group++)
        {
            for (int i = 0; i < 20; i++)
            {
                _testDataService.AddOrUpdateTestResult(
                    CreateTestResult(
                        $"large_{group}_{i}",
                        $"Test.Large{group}_{i}",
                        errorTypes[group],
                        TestStatus.Fail,
                        timestamp: now));
            }
        }

        // Act
        var failed = _testDataService.GetAllTestResults().Where(t => t.Status == TestStatus.Fail);
        var groups = _groupingService.GroupFailures(failed, 0.8);

        // Assert
        groups.Count.ShouldBe(5);
        groups.Sum(g => g.TestCount).ShouldBe(100);
        groups.All(g => g.TestCount == 20).ShouldBeTrue();
    }

    #endregion
}
