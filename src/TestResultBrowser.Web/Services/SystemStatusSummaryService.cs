using Microsoft.Extensions.Caching.Memory;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

public sealed class SystemStatusSummaryService : ISystemStatusSummaryService
{
    private const string CacheKey = "system-status-snapshot";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly IMemoryCache _cache;
    private readonly ITestDataService _testDataService;
    private readonly ILogger<SystemStatusSummaryService> _logger;

    public SystemStatusSummaryService(
        IMemoryCache cache,
        ITestDataService testDataService,
        ILogger<SystemStatusSummaryService> logger)
    {
        _cache = cache;
        _testDataService = testDataService;
        _logger = logger;
    }

    public Task<SystemStatusSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await Task.Run(BuildSnapshot, cancellationToken);
        })!;
    }

    public void Invalidate()
    {
        _cache.Remove(CacheKey);
    }

    private SystemStatusSnapshot BuildSnapshot()
    {
        _logger.LogDebug("Building cached system status snapshot");

        var buildIds = _testDataService.GetAllBuildIds().ToList();
        var configurations = _testDataService.GetAllConfigurationIds().ToList();
        var domains = _testDataService.GetAllDomainIds().ToList();
        var dateRange = _testDataService.GetDateRange();

        return new SystemStatusSnapshot
        {
            TotalResults = _testDataService.GetTotalCount(),
            MemoryUsageBytes = _testDataService.GetApproximateMemoryUsage(),
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