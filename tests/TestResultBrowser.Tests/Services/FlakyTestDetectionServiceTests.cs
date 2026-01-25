using Shouldly;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;
using Microsoft.Extensions.Logging;
using TestResultBrowser.Tests.Utilities;

namespace TestResultBrowser.Tests.Services;

/// <summary>
/// Unit tests for FlakyTestDetectionService
/// </summary>
public class FlakyTestDetectionServiceTests
{
    private readonly FlakyTestDetectionService _service;
    private readonly ILogger<FlakyTestDetectionService> _logger;

    public FlakyTestDetectionServiceTests()
    {
        _logger = new MockLogger<FlakyTestDetectionService>();
        _service = new FlakyTestDetectionService(_logger);
    }

    private TestResult CreateTestResult(
        string id,
        string testName,
        TestStatus status,
        DateTime? timestamp = null,
        string configId = "Config1")
    {
        return new TestResult
        {
            Id = id,
            TestFullName = testName,
            ClassName = "TestClass",
            MethodName = "TestMethod",
            Status = status,
            ErrorMessage = status == TestStatus.Fail ? "Test failed" : null,
            ExecutionTimeSeconds = 1.0,
            Timestamp = timestamp ?? DateTime.UtcNow,
            DomainId = "Domain1",
            FeatureId = "Feature1",
            TestSuiteId = "TestSuite",
            ConfigurationId = configId,
            BuildId = "Build-100",
            BuildNumber = 100,
            Machine = "TestMachine",
            Feature = "Feature1",
            WorkItemIds = new List<string>(),
            ReportDirectoryPath = null
        };
    }

    #region Detection Tests

    [Fact]
    public void DetectFlakyTests_WithMixedPassFail_IdentifiesFlaky()
    {
        // Arrange - 10 runs: 5 pass, 5 fail = 50% flaky
        var now = DateTime.UtcNow;
        var results = new List<TestResult>();
        for (int i = 0; i < 5; i++)
        {
            results.Add(CreateTestResult($"id_{i*2}", "Test.Flaky", TestStatus.Pass, now.AddHours(-i)));
            results.Add(CreateTestResult($"id_{i*2+1}", "Test.Flaky", TestStatus.Fail, now.AddHours(-i-0.5)));
        }

        // Act
        var flakyTests = _service.DetectFlakyTests(results, failureRateThreshold: 0.20);

        // Assert
        flakyTests.ShouldHaveSingleItem();
        flakyTests[0].TestFullName.ShouldBe("Test.Flaky");
        flakyTests[0].FailureRate.ShouldBe(0.5, 0.01);
        flakyTests[0].FailureCount.ShouldBe(5);
        flakyTests[0].PassCount.ShouldBe(5);
    }

    [Fact]
    public void DetectFlakyTests_WithAllPass_DoesNotFlag()
    {
        // Arrange - 10 consecutive passes
        var now = DateTime.UtcNow;
        var results = Enumerable.Range(0, 10)
            .Select(i => CreateTestResult($"id_{i}", "Test.Stable", TestStatus.Pass, now.AddHours(-i)))
            .ToList();

        // Act
        var flakyTests = _service.DetectFlakyTests(results, failureRateThreshold: 0.20);

        // Assert
        flakyTests.ShouldBeEmpty();
    }

    [Fact]
    public void DetectFlakyTests_WithAllFail_DoesNotFlag()
    {
        // Arrange - 10 consecutive failures
        var now = DateTime.UtcNow;
        var results = Enumerable.Range(0, 10)
            .Select(i => CreateTestResult($"id_{i}", "Test.Broken", TestStatus.Fail, now.AddHours(-i)))
            .ToList();

        // Act
        var flakyTests = _service.DetectFlakyTests(results, failureRateThreshold: 0.20);

        // Assert
        flakyTests.ShouldBeEmpty();
    }

    [Fact]
    public void DetectFlakyTests_WithLowFailureRate_DoesNotFlag()
    {
        // Arrange - 10 runs: 1 fail, 9 pass = 10% failure (below 20% threshold)
        var now = DateTime.UtcNow;
        var results = new List<TestResult>
        {
            CreateTestResult("id_0", "Test.LowFlaky", TestStatus.Fail, now)
        };
        for (int i = 1; i < 10; i++)
        {
            results.Add(CreateTestResult($"id_{i}", "Test.LowFlaky", TestStatus.Pass, now.AddHours(-i)));
        }

        // Act
        var flakyTests = _service.DetectFlakyTests(results, failureRateThreshold: 0.20);

        // Assert
        flakyTests.ShouldBeEmpty();
    }

    [Fact]
    public void DetectFlakyTests_WithMultipleTests_SortsByFailureRate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var results = new List<TestResult>();

        // Test 1: 30% failure rate (3/10)
        for (int i = 0; i < 10; i++)
        {
            results.Add(CreateTestResult($"t1_id_{i}", "Test.High", 
                i < 3 ? TestStatus.Fail : TestStatus.Pass, now.AddHours(-i)));
        }

        // Test 2: 20% failure rate (2/10)
        for (int i = 0; i < 10; i++)
        {
            results.Add(CreateTestResult($"t2_id_{i}", "Test.Medium", 
                i < 2 ? TestStatus.Fail : TestStatus.Pass, now.AddHours(-i)));
        }

        // Act
        var flakyTests = _service.DetectFlakyTests(results, failureRateThreshold: 0.15);

        // Assert
        flakyTests.Count.ShouldBe(2);
        flakyTests[0].TestFullName.ShouldBe("Test.High");
        flakyTests[0].FailureRate.ShouldBeGreaterThan(flakyTests[1].FailureRate);
    }

    [Fact]
    public void DetectFlakyTests_RespectsRecentRunWindow()
    {
        // Arrange - create 25 runs with interleaved failures, window=10 should pick last 10 most recent
        var now = DateTime.UtcNow;
        var results = new List<TestResult>();

        // Create 25 runs: alternating pass/fail pattern
        // Most recent (now): fail, pass, fail, pass, ... (10 most recent should be ~50% fail)
        for (int i = 24; i >= 0; i--)
        {
            var status = i % 2 == 0 ? TestStatus.Fail : TestStatus.Pass;
            results.Add(CreateTestResult($"id_{i}", "Test.Window", status, now.AddHours(-(24 - i))));
        }

        // Act
        var flakyTests = _service.DetectFlakyTests(results, failureRateThreshold: 0.20, recentRunWindow: 10);

        // Assert - should detect as flaky because last 10 runs have significant failures
        flakyTests.ShouldHaveSingleItem();
        flakyTests[0].TotalRuns.ShouldBe(10);
    }

    #endregion

    #region Filtering Tests

    [Fact]
    public void FilterFlakyTests_ByConfiguration_ReturnsMatchingOnly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var reports = new List<FlakyTestReport>
        {
            new FlakyTestReport(
                "Test.A", 0.5, 10, 5, 5, TestStatus.Fail, now, now,
                TrendDirection.Stable,
                new List<TestResult>
                {
                    CreateTestResult("id_1", "Test.A", TestStatus.Fail, configId: "Config1"),
                    CreateTestResult("id_2", "Test.A", TestStatus.Pass, configId: "Config1")
                }
            ),
            new FlakyTestReport(
                "Test.B", 0.4, 10, 4, 6, TestStatus.Pass, now, now,
                TrendDirection.Stable,
                new List<TestResult>
                {
                    CreateTestResult("id_3", "Test.B", TestStatus.Fail, configId: "Config2"),
                    CreateTestResult("id_4", "Test.B", TestStatus.Pass, configId: "Config2")
                }
            )
        };

        // Act
        var filtered = _service.FilterFlakyTests(reports, configurationFilter: "Config1");

        // Assert
        filtered.Count.ShouldBe(1);
        filtered[0].TestFullName.ShouldBe("Test.A");
    }

    [Fact]
    public void FilterFlakyTests_ByTrend_ReturnsMatchingOnly()
    {
        // Arrange
        var reports = new List<FlakyTestReport>
        {
            new FlakyTestReport("Test.A", 0.5, 10, 5, 5, TestStatus.Fail, DateTime.UtcNow, DateTime.UtcNow,
                TrendDirection.Improving, new List<TestResult>()),
            new FlakyTestReport("Test.B", 0.4, 10, 4, 6, TestStatus.Pass, DateTime.UtcNow, DateTime.UtcNow,
                TrendDirection.Worsening, new List<TestResult>())
        };

        // Act
        var filtered = _service.FilterFlakyTests(reports, trendFilter: TrendDirection.Improving);

        // Assert
        filtered.Count.ShouldBe(1);
        filtered[0].Trend.ShouldBe(TrendDirection.Improving);
    }

    #endregion

    #region Trend Tests

    [Fact]
    public void DetectFlakyTests_CalculatesTrend_Correctly()
    {
        // Arrange - first 5 runs (older): all pass
        // second 5 runs (newer): all fail = Worsening trend
        var now = DateTime.UtcNow;
        var results = new List<TestResult>();

        // First 5 runs (older, from 10 hours to 6 hours ago): all pass
        for (int i = 0; i < 5; i++)
        {
            results.Add(CreateTestResult($"id_{i}", "Test.Trend", TestStatus.Pass, now.AddHours(-(10 - i))));
        }

        // Last 5 runs (newer, from 5 hours to 1 hour ago): all fail
        for (int i = 5; i < 10; i++)
        {
            results.Add(CreateTestResult($"id_{i}", "Test.Trend", TestStatus.Fail, now.AddHours(-(10 - i))));
        }

        // Act
        var flakyTests = _service.DetectFlakyTests(results, failureRateThreshold: 0.20);

        // Assert - should detect Worsening trend (0% -> 100% failure rate)
        flakyTests.ShouldHaveSingleItem();
        flakyTests[0].Trend.ShouldBe(TrendDirection.Worsening);
    }

    #endregion
}
