using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Services;

public class SystemStatusSummaryServiceTests : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly Mock<ITestDataService> _testDataServiceMock;
    private readonly Mock<ILogger<SystemStatusSummaryService>> _loggerMock;
    private readonly SystemStatusSummaryService _service;

    public SystemStatusSummaryServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _testDataServiceMock = new Mock<ITestDataService>();
        _loggerMock = new Mock<ILogger<SystemStatusSummaryService>>();

        SetupDefaultMocks();

        _service = new SystemStatusSummaryService(_cache, _testDataServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    private void SetupDefaultMocks(
        int totalCount = 42,
        long memoryUsage = 1024 * 1024,
        string[]? buildIds = null,
        string[]? configIds = null,
        string[]? domainIds = null,
        DateTime? earliest = null,
        DateTime? latest = null,
        string[]? versions = null,
        string[]? namedConfigs = null)
    {
        buildIds ??= new[] { "Build-1", "Build-2", "Build-3" };
        configIds ??= new[] { "Config-A", "Config-B" };
        domainIds ??= new[] { "Domain-X", "Domain-Y", "Domain-Z" };
        versions ??= new[] { "1.0.0", "1.1.0" };
        namedConfigs ??= new[] { "Release", "Debug" };

        _testDataServiceMock.Setup(s => s.GetTotalCount()).Returns(totalCount);
        _testDataServiceMock.Setup(s => s.GetApproximateMemoryUsage()).Returns(memoryUsage);
        _testDataServiceMock.Setup(s => s.GetAllBuildIds()).Returns(buildIds);
        _testDataServiceMock.Setup(s => s.GetAllConfigurationIds()).Returns(configIds);
        _testDataServiceMock.Setup(s => s.GetAllDomainIds()).Returns(domainIds);
        _testDataServiceMock.Setup(s => s.GetDateRange()).Returns((earliest, latest));
        _testDataServiceMock.Setup(s => s.GetAllVersions()).Returns(versions);
        _testDataServiceMock.Setup(s => s.GetAllNamedConfigs()).Returns(namedConfigs);
    }

    #region GetSnapshotAsync - Data mapping

    [Fact]
    public async Task GetSnapshotAsync_MapsTestDataServiceValues_ToSnapshot()
    {
        // Act
        var snapshot = await _service.GetSnapshotAsync();

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.TotalResults.ShouldBe(42);
        snapshot.MemoryUsageBytes.ShouldBe(1024 * 1024);
        snapshot.TotalBuilds.ShouldBe(3);
        snapshot.TotalConfigurations.ShouldBe(2);
        snapshot.TotalDomains.ShouldBe(3);
    }

    [Fact]
    public async Task GetSnapshotAsync_PopulatesVersions_FromTestDataService()
    {
        // Arrange
        SetupDefaultMocks(versions: new[] { "2.0.0", "2.1.0", "3.0.0" });
        var service = new SystemStatusSummaryService(_cache, _testDataServiceMock.Object, _loggerMock.Object);

        // Act
        var snapshot = await service.GetSnapshotAsync();

        // Assert
        snapshot.Versions.ShouldBe(new[] { "2.0.0", "2.1.0", "3.0.0" });
    }

    [Fact]
    public async Task GetSnapshotAsync_PopulatesNamedConfigs_FromTestDataService()
    {
        // Arrange
        SetupDefaultMocks(namedConfigs: new[] { "Release", "Debug", "Staging" });
        var service = new SystemStatusSummaryService(_cache, _testDataServiceMock.Object, _loggerMock.Object);

        // Act
        var snapshot = await service.GetSnapshotAsync();

        // Assert
        snapshot.NamedConfigs.ShouldBe(new[] { "Release", "Debug", "Staging" });
    }

    [Fact]
    public async Task GetSnapshotAsync_PopulatesEarliestAndLatestDate_WhenAvailable()
    {
        // Arrange
        var earliest = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var latest = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        SetupDefaultMocks(earliest: earliest, latest: latest);
        var service = new SystemStatusSummaryService(_cache, _testDataServiceMock.Object, _loggerMock.Object);

        // Act
        var snapshot = await service.GetSnapshotAsync();

        // Assert
        snapshot.EarliestDate.ShouldBe(earliest);
        snapshot.LatestDate.ShouldBe(latest);
    }

    [Fact]
    public async Task GetSnapshotAsync_SetsEarliestAndLatestToNull_WhenNoDateRange()
    {
        // Arrange
        SetupDefaultMocks(earliest: null, latest: null);
        var service = new SystemStatusSummaryService(_cache, _testDataServiceMock.Object, _loggerMock.Object);

        // Act
        var snapshot = await service.GetSnapshotAsync();

        // Assert
        snapshot.EarliestDate.ShouldBeNull();
        snapshot.LatestDate.ShouldBeNull();
    }

    [Fact]
    public async Task GetSnapshotAsync_LimitsRecentBuilds_To10()
    {
        // Arrange
        var manyBuilds = Enumerable.Range(1, 15).Select(i => $"Build-{i}").ToArray();
        SetupDefaultMocks(buildIds: manyBuilds);
        var service = new SystemStatusSummaryService(_cache, _testDataServiceMock.Object, _loggerMock.Object);

        // Act
        var snapshot = await service.GetSnapshotAsync();

        // Assert
        snapshot.TotalBuilds.ShouldBe(15);
        snapshot.RecentBuilds.Count.ShouldBe(10);
        snapshot.RecentBuilds[0].ShouldBe("Build-1");
        snapshot.RecentBuilds[9].ShouldBe("Build-10");
    }

    [Fact]
    public async Task GetSnapshotAsync_ReturnsAllRecentBuilds_WhenFewerThan10()
    {
        // Arrange
        var fewBuilds = new[] { "Build-A", "Build-B", "Build-C" };
        SetupDefaultMocks(buildIds: fewBuilds);
        var service = new SystemStatusSummaryService(_cache, _testDataServiceMock.Object, _loggerMock.Object);

        // Act
        var snapshot = await service.GetSnapshotAsync();

        // Assert
        snapshot.RecentBuilds.ShouldBe(fewBuilds);
    }

    [Fact]
    public async Task GetSnapshotAsync_ReturnsEmptyRecentBuilds_WhenNoBuilds()
    {
        // Arrange
        SetupDefaultMocks(buildIds: Array.Empty<string>());
        var service = new SystemStatusSummaryService(_cache, _testDataServiceMock.Object, _loggerMock.Object);

        // Act
        var snapshot = await service.GetSnapshotAsync();

        // Assert
        snapshot.TotalBuilds.ShouldBe(0);
        snapshot.RecentBuilds.ShouldBeEmpty();
    }

    #endregion

    #region GetSnapshotAsync - Caching behavior

    [Fact]
    public async Task GetSnapshotAsync_ReturnsCachedSnapshot_OnSecondCall()
    {
        // Act
        var first = await _service.GetSnapshotAsync();
        var second = await _service.GetSnapshotAsync();

        // Assert - same object reference proves it came from cache
        second.ShouldBeSameAs(first);
        _testDataServiceMock.Verify(s => s.GetTotalCount(), Times.Once);
    }

    [Fact]
    public async Task GetSnapshotAsync_DoesNotCallTestDataService_MultipleTimesWithoutInvalidation()
    {
        // Act
        await _service.GetSnapshotAsync();
        await _service.GetSnapshotAsync();
        await _service.GetSnapshotAsync();

        // Assert - underlying data service called only once (cache hit on subsequent calls)
        _testDataServiceMock.Verify(s => s.GetAllBuildIds(), Times.Once);
        _testDataServiceMock.Verify(s => s.GetAllConfigurationIds(), Times.Once);
        _testDataServiceMock.Verify(s => s.GetAllDomainIds(), Times.Once);
    }

    #endregion

    #region Invalidate

    [Fact]
    public async Task Invalidate_CausesGetSnapshotAsync_ToRebuildSnapshot()
    {
        // Arrange - get initial snapshot
        var initial = await _service.GetSnapshotAsync();

        // Arrange - change underlying data
        _testDataServiceMock.Setup(s => s.GetTotalCount()).Returns(999);

        // Act
        _service.Invalidate();
        var refreshed = await _service.GetSnapshotAsync();

        // Assert - new snapshot reflects updated data
        refreshed.TotalResults.ShouldBe(999);
        refreshed.ShouldNotBeSameAs(initial);
    }

    [Fact]
    public async Task Invalidate_CausesTestDataService_ToBeCalledAgain()
    {
        // Arrange
        await _service.GetSnapshotAsync();
        _testDataServiceMock.Invocations.Clear();

        // Act
        _service.Invalidate();
        await _service.GetSnapshotAsync();

        // Assert - data service was called again after invalidation
        _testDataServiceMock.Verify(s => s.GetTotalCount(), Times.Once);
    }

    [Fact]
    public void Invalidate_WithNoExistingCache_DoesNotThrow()
    {
        // Act & Assert - should not throw when nothing is cached
        Should.NotThrow(() => _service.Invalidate());
    }

    [Fact]
    public async Task Invalidate_CalledTwice_AllowsRebuildOnBothSubsequentCalls()
    {
        // Arrange
        await _service.GetSnapshotAsync();

        _service.Invalidate();
        await _service.GetSnapshotAsync();
        _testDataServiceMock.Invocations.Clear();

        // Act - second invalidation and rebuild
        _service.Invalidate();
        await _service.GetSnapshotAsync();

        // Assert
        _testDataServiceMock.Verify(s => s.GetTotalCount(), Times.Once);
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task GetSnapshotAsync_WithAlreadyCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await _service.GetSnapshotAsync(cts.Token));
    }

    #endregion

    #region Zero / empty data

    [Fact]
    public async Task GetSnapshotAsync_WithAllZeroData_ReturnsZeroSnapshot()
    {
        // Arrange
        SetupDefaultMocks(
            totalCount: 0,
            memoryUsage: 0,
            buildIds: Array.Empty<string>(),
            configIds: Array.Empty<string>(),
            domainIds: Array.Empty<string>(),
            versions: Array.Empty<string>(),
            namedConfigs: Array.Empty<string>());
        var service = new SystemStatusSummaryService(_cache, _testDataServiceMock.Object, _loggerMock.Object);

        // Act
        var snapshot = await service.GetSnapshotAsync();

        // Assert
        snapshot.TotalResults.ShouldBe(0);
        snapshot.MemoryUsageBytes.ShouldBe(0);
        snapshot.TotalBuilds.ShouldBe(0);
        snapshot.TotalConfigurations.ShouldBe(0);
        snapshot.TotalDomains.ShouldBe(0);
        snapshot.RecentBuilds.ShouldBeEmpty();
        snapshot.Versions.ShouldBeEmpty();
        snapshot.NamedConfigs.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSnapshotAsync_WithExactly10Builds_ReturnsAll10AsRecentBuilds()
    {
        // Arrange
        var tenBuilds = Enumerable.Range(1, 10).Select(i => $"Build-{i}").ToArray();
        SetupDefaultMocks(buildIds: tenBuilds);
        var service = new SystemStatusSummaryService(_cache, _testDataServiceMock.Object, _loggerMock.Object);

        // Act
        var snapshot = await service.GetSnapshotAsync();

        // Assert
        snapshot.RecentBuilds.Count.ShouldBe(10);
        snapshot.TotalBuilds.ShouldBe(10);
    }

    #endregion
}