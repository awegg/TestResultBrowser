using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

public sealed class SystemStatusSummaryService : ISystemStatusSummaryService
{
    private const string CacheKey = "system-status-snapshot";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly IMemoryCache _cache;
    private readonly ITestDataService _testDataService;
    private readonly ILogger<SystemStatusSummaryService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SystemStatusSummaryService"/> with its required dependencies.
    /// </summary>
    /// <param name="cache">The memory cache used to store the aggregated system status snapshot.</param>
    /// <param name="testDataService">Service used to query test-data metrics and identifiers for building the snapshot.</param>
    /// <param name="logger">Logger used for diagnostic messages related to snapshot building and cache activity.</param>
    public SystemStatusSummaryService(
        IMemoryCache cache,
        ITestDataService testDataService,
        ILogger<SystemStatusSummaryService> logger)
    {
        _cache = cache;
        _testDataService = testDataService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a cached SystemStatusSnapshot, creating and caching a new snapshot if none exists.
    /// </summary>
    /// <param name="cancellationToken">Optional token to cancel building the snapshot when a cache miss occurs.</param>
    /// <returns>The cached SystemStatusSnapshot, or a newly built snapshot that has been stored in the cache.</returns>
    public Task<SystemStatusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await Task.Run(BuildSnapshot, cancellationToken);
        })!;
    }

    /// <summary>
    /// Removes the cached system status snapshot from the memory cache.
    /// </summary>
    /// <remarks>
    /// Does not rebuild the snapshot; subsequent calls to GetSnapshotAsync will recreate it on demand.
    /// </remarks>
    public void Invalidate()
    {
        _cache.Remove(CacheKey);
    }

    /// <summary>
    /// Builds an aggregated SystemStatusSnapshot representing current totals, date range, recent builds, versions, and named configurations.
    /// </summary>
    /// <returns>A SystemStatusSnapshot containing totals (results, builds, configurations, domains), approximate memory usage, earliest and latest dates, up to 10 recent build IDs, versions, and named configurations.</returns>
    private SystemStatusSnapshot BuildSnapshot()
    {
        _logger.LogDebug("Building cached system status snapshot");

        var buildIds = _testDataService.GetAllBuildIds().ToList();
        var configurations = _testDataService.GetAllConfigurationIds().ToList();
        var domains = _testDataService.GetAllDomainIds().ToList();
        var dateRange = _testDataService.GetDateRange();
        var managedMemoryBytes = GC.GetTotalMemory(forceFullCollection: false);
        long processMemoryBytes;

        try
        {
            using var process = Process.GetCurrentProcess();
            processMemoryBytes = process.WorkingSet64;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falling back to managed heap size for process memory metric");
            processMemoryBytes = managedMemoryBytes;
        }

        return new SystemStatusSnapshot
        {
            TotalResults = _testDataService.GetTotalCount(),
            MemoryUsageBytes = _testDataService.GetApproximateMemoryUsage(),
            ManagedMemoryBytes = managedMemoryBytes,
            ProcessMemoryBytes = processMemoryBytes,
            TotalBuilds = buildIds.Count,
            TotalConfigurations = configurations.Count,
            TotalDomains = domains.Count,
            EarliestDate = dateRange.Earliest,
            LatestDate = dateRange.Latest,
            RecentBuilds = buildIds.Take(10).ToList(),
            Versions = _testDataService.GetAllVersions().ToList(),
            NamedConfigs = _testDataService.GetAllNamedConfigs().ToList()
        };
    }
}