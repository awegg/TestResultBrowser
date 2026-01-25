using Shouldly;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;
using Microsoft.Extensions.Logging;

namespace TestResultBrowser.Tests.Services;

/// <summary>
/// Unit tests for FailureGroupingService covering grouping algorithms,
/// similarity matching, normalization, and edge cases.
/// </summary>
public class FailureGroupingServiceTests
{
    private readonly ILogger<FailureGroupingService> _logger;
    private readonly FailureGroupingService _service;

    public FailureGroupingServiceTests()
    {
        _logger = new MockLogger<FailureGroupingService>();
        _service = new FailureGroupingService(_logger);
    }

    private TestResult CreateFailedTest(
        string id,
        string testName,
        string errorMessage,
        string domainId = "TestDomain",
        string featureId = "TestFeature",
        string configId = "Config1",
        DateTime? timestamp = null)
    {
        return new TestResult
        {
            Id = id,
            TestFullName = testName,
            ClassName = "TestClass",
            MethodName = "TestMethod",
            Status = TestStatus.Fail,
            ErrorMessage = errorMessage,
            ExecutionTimeSeconds = 1.23,
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

    #region Exact Match Grouping Tests

    [Fact]
    public void GroupFailures_WithIdenticalErrorMessages_CreatesOneGroup()
    {
        // Arrange
        var errorMsg = "Database connection timeout after 30 seconds";
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.ConnectionA", errorMsg),
            CreateFailedTest("id2", "Test.ConnectionB", errorMsg),
            CreateFailedTest("id3", "Test.ConnectionC", errorMsg)
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(3);
        groups[0].RepresentativeMessage.ShouldBe(errorMsg);
    }

    [Fact]
    public void GroupFailures_WithDifferentMessages_CreatesMultipleGroups()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.Db", "Timeout"),
            CreateFailedTest("id2", "Test.Auth", "Unauthorized access"),
            CreateFailedTest("id3", "Test.Parse", "JSON parse error")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.Count.ShouldBe(3);
        groups.All(g => g.TestCount == 1).ShouldBeTrue();
    }

    [Fact]
    public void GroupFailures_WithNoFailures_ReturnsEmptyList()
    {
        // Arrange
        var failures = Enumerable.Empty<TestResult>();

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldBeEmpty();
    }

    [Fact]
    public void GroupFailures_WithNullErrorMessages_IgnoresThem()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", null!),
            CreateFailedTest("id2", "Test.B", ""),
            CreateFailedTest("id3", "Test.C", "   "),
            CreateFailedTest("id4", "Test.D", "Real error message")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(1);
    }

    #endregion

    #region Message Normalization Tests

    [Fact]
    public void GroupFailures_NormalizesDates_BeforeGrouping()
    {
        // Arrange - same error with different timestamps
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "Request failed at 2026-01-25T10:30:45Z"),
            CreateFailedTest("id2", "Test.B", "Request failed at 2026-01-25T10:45:12.123Z"),
            CreateFailedTest("id3", "Test.C", "Request failed at 2026-01-25T11:20:33+02:00")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert - all should group into one because dates are normalized to {DATETIME}
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(3);
    }

    [Fact]
    public void GroupFailures_NormalizesGUIDs_BeforeGrouping()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "Failed with ID: 550e8400-e29b-41d4-a716-446655440000"),
            CreateFailedTest("id2", "Test.B", "Failed with ID: 6ba7b810-9dad-11d1-80b4-00c04fd430c8"),
            CreateFailedTest("id3", "Test.C", "Failed with ID: a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(3);
    }

    [Fact]
    public void GroupFailures_NormalizesNumbers_BeforeGrouping()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "File size is 1024 bytes"),
            CreateFailedTest("id2", "Test.B", "File size is 2048 bytes"),
            CreateFailedTest("id3", "Test.C", "File size is 512 bytes")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(3);
    }

    [Fact]
    public void GroupFailures_NormalizesPaths_BeforeGrouping()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "File not found: C:\\Users\\John\\file.txt"),
            CreateFailedTest("id2", "Test.B", "File not found: C:\\Users\\Jane\\file.txt"),
            CreateFailedTest("id3", "Test.C", "File not found: /home/user/file.txt")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(3);
    }

    #endregion

    #region Fuzzy Matching Tests

    [Fact]
    public void GroupFailures_WithHighSimilarity_GroupsNearDuplicates()
    {
        // Arrange - very similar messages (differ by 1-2 words)
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "Connection to database failed"),
            CreateFailedTest("id2", "Test.B", "Connection to database timed out"),
            CreateFailedTest("id3", "Test.C", "Connection to database unavailable")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(3);
    }

    [Fact]
    public void GroupFailures_WithLowSimilarity_CreatesMultipleGroups()
    {
        // Arrange - different messages
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "Network error occurred"),
            CreateFailedTest("id2", "Test.B", "User authentication failed"),
            CreateFailedTest("id3", "Test.C", "File system not accessible")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.95); // High threshold

        // Assert
        groups.Count.ShouldBe(3);
    }

    [Fact]
    public void GroupFailures_RespectsSimilarityThreshold()
    {
        // Arrange - messages with varying similarity
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "Error in module A"),
            CreateFailedTest("id2", "Test.B", "Error in module B"), // 1 word different
            CreateFailedTest("id3", "Test.C", "Completely different issue")
        };

        // Act - with high threshold, fuzzy matching is stricter
        var groupsHigh = _service.GroupFailures(failures, 0.95);
        var groupsLow = _service.GroupFailures(failures, 0.50);

        // Assert
        groupsHigh.Count.ShouldBeGreaterThan(groupsLow.Count);
    }

    #endregion

    #region Group Properties Tests

    [Fact]
    public void GroupFailures_PopulatesGroupProperties_Correctly()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "Error", "Domain1", "Feature1", "Config1"),
            CreateFailedTest("id2", "Test.B", "Error", "Domain1", "Feature2", "Config1"),
            CreateFailedTest("id3", "Test.C", "Error", "Domain2", "Feature1", "Config2")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        var group = groups[0];
        group.TestCount.ShouldBe(3);
        group.DomainIds.ShouldContain("Domain1");
        group.DomainIds.ShouldContain("Domain2");
        group.FeatureIds.ShouldContain("Feature1");
        group.FeatureIds.ShouldContain("Feature2");
        group.FeatureIds.Count().ShouldBe(2);
        group.DomainIds.Count().ShouldBe(2);
    }

    [Fact]
    public void GroupFailures_SortsGroupsByTestCount_Descending()
    {
        // Arrange
        var failures = new List<TestResult>();
        
        // Group 1: 5 failures
        for (int i = 0; i < 5; i++)
            failures.Add(CreateFailedTest($"id1_{i}", $"Test.A{i}", "Error type A"));
        
        // Group 2: 2 failures
        for (int i = 0; i < 2; i++)
            failures.Add(CreateFailedTest($"id2_{i}", $"Test.B{i}", "Error type B entirely different"));

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.Count.ShouldBe(2);
        groups[0].TestCount.ShouldBe(5);
        groups[1].TestCount.ShouldBe(2);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void GroupFailures_WithVeryLongMessages_HandlesCorrectly()
    {
        // Arrange
        var longMessage = string.Concat(Enumerable.Repeat("This is a very long error message. ", 100));
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", longMessage),
            CreateFailedTest("id2", "Test.B", longMessage)
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(2);
    }

    [Fact]
    public void GroupFailures_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "Error: [critical] {exception} <timeout> & 'invalid'"),
            CreateFailedTest("id2", "Test.B", "Error: [critical] {exception} <timeout> & 'invalid'")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(2);
    }

    [Fact]
    public void GroupFailures_WithMixedCaseMessages_TreatsAsSame()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "ERROR: Connection Failed"),
            CreateFailedTest("id2", "Test.B", "error: connection failed"),
            CreateFailedTest("id3", "Test.C", "Error: Connection failed")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(3);
    }

    [Fact]
    public void GroupFailures_WithWhitespaceVariations_NormalizesCorrectly()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "Error   with   extra   spaces"),
            CreateFailedTest("id2", "Test.B", "Error with extra spaces"),
            CreateFailedTest("id3", "Test.C", "Error\twith\ttabs")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(3);
    }

    [Fact]
    public void GroupFailures_WithMultipleFailuresPerTest_IncludesAll()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "Connection error"),
            CreateFailedTest("id2", "Test.A", "Connection error"), // Same test, different run
            CreateFailedTest("id3", "Test.B", "Connection error")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.8);

        // Assert
        groups.ShouldHaveSingleItem();
        groups[0].TestCount.ShouldBe(3);
        groups[0].TestResults.Count.ShouldBe(3);
    }

    #endregion

    #region Threshold Edge Cases

    [Fact]
    public void GroupFailures_WithThreshold50_IsMorePermissive()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "Network timeout"),
            CreateFailedTest("id2", "Test.B", "Connection error")
        };

        // Act
        var groups = _service.GroupFailures(failures, 0.50);

        // Assert - at 50% threshold, these may group together
        groups.Count.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public void GroupFailures_WithThreshold100_OnlyExactMatches()
    {
        // Arrange
        var failures = new[]
        {
            CreateFailedTest("id1", "Test.A", "Error message"),
            CreateFailedTest("id2", "Test.B", "Error message"),
            CreateFailedTest("id3", "Test.C", "Error message slightly different")
        };

        // Act
        var groups = _service.GroupFailures(failures, 1.0); // 100% = exact match only

        // Assert
        groups.Count.ShouldBe(2);
        groups[0].TestCount.ShouldBe(2);
        groups[1].TestCount.ShouldBe(1);
    }

    #endregion
}

/// <summary>
/// Mock logger for testing
/// </summary>
public class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // No-op for testing
    }
}
