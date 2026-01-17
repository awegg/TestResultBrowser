using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using TestResultBrowser.Web.Hubs;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Configuration options for the test result browser
/// </summary>
public class TestResultBrowserOptions
{
    /// <summary>
    /// Path to the file share containing test results
    /// Example: \\shared-server\test-results
    /// </summary>
    public string FileSharePath { get; set; } = string.Empty;

    /// <summary>
    /// Polling interval in minutes for file watcher
    /// Default: 15 minutes
    /// </summary>
    public int PollingIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Flaky test detection thresholds
    /// </summary>
    public FlakyTestThresholds FlakyTestThresholds { get; set; } = new();

    /// <summary>
    /// Base URL for Polarion integration
    /// </summary>
    public string PolarionBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Cache configuration settings
    /// </summary>
    public CacheSettings Cache { get; set; } = new();
}

/// <summary>
/// Flaky test detection threshold configuration
/// </summary>
public class FlakyTestThresholds
{
    /// <summary>
    /// Number of recent runs to analyze for flaky detection
    /// Default: 20
    /// </summary>
    public int RollingWindowSize { get; set; } = 20;

    /// <summary>
    /// Percentage of failures in rolling window to trigger flaky status (0-100)
    /// Default: 30
    /// </summary>
    public int FlakinessTriggerPercentage { get; set; } = 30;

    /// <summary>
    /// Number of consecutive passes required to clear flaky status
    /// Default: 10
    /// </summary>
    public int ClearAfterConsecutivePasses { get; set; } = 10;
}

/// <summary>
/// Cache configuration settings
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Maximum memory usage for cache in GB
    /// Default: 15
    /// </summary>
    public int MaxMemoryGB { get; set; } = 15;

    /// <summary>
    /// Maximum memory for data cache in MB (for stricter limits)
    /// Default: 512 MB
    /// </summary>
    public int MaxMemoryCacheMB { get; set; } = 512;

    /// <summary>
    /// Aggregation cache duration in minutes
    /// Default: 5
    /// </summary>
    public int AggregationCacheMinutes { get; set; } = 5;
}

/// <summary>
/// Background service for monitoring file system and importing test results
/// Uses timer-based polling (reliable over UNC network shares)
/// </summary>
public class FileWatcherService : BackgroundService, IFileWatcherService
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TestResultBrowserOptions _options;
    private readonly IHubContext<TestDataHub> _hubContext;
    private Timer? _timer;

    public DateTime? LastScanTime { get; private set; }
    public int LastScanFileCount { get; private set; }
    public bool IsScanningInProgress { get; private set; }

    public FileWatcherService(
        ILogger<FileWatcherService> logger,
        IServiceProvider serviceProvider,
        IOptions<TestResultBrowserOptions> options,
        IHubContext<TestDataHub> hubContext)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileWatcherService starting. Polling interval: {Interval} minutes", _options.PollingIntervalMinutes);

        // Perform initial scan immediately
        await ScanNowAsync();

        // Setup timer for periodic scans
        var interval = TimeSpan.FromMinutes(_options.PollingIntervalMinutes);
        _timer = new Timer(
            callback: async _ => await ScanNowAsync(),
            state: null,
            dueTime: interval,
            period: interval);

        // Keep service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public async Task ScanNowAsync()
    {
        if (IsScanningInProgress)
        {
            _logger.LogWarning("Scan already in progress, skipping requested scan");
            return;
        }

        try
        {
            IsScanningInProgress = true;
            var startTime = DateTime.UtcNow;
            var fileCount = 0;

            _logger.LogInformation("Starting manual file system scan of {Path}", _options.FileSharePath);

            if (!Directory.Exists(_options.FileSharePath))
            {
                _logger.LogError("File share path does not exist: {Path}", _options.FileSharePath);
                return;
            }

            // Retry logic with exponential backoff for network resilience
            var xmlFiles = await RetryWithBackoffAsync(
                () => Task.FromResult(Directory.GetFiles(
                    _options.FileSharePath,
                    "*.xml",
                    SearchOption.AllDirectories)),
                maxRetries: 3,
                operationName: "Directory.GetFiles");

            _logger.LogInformation("Found {Count} XML files to process", xmlFiles.Length);

            // Process files in batches using scoped services
            using var scope = _serviceProvider.CreateScope();
            var filePathParser = scope.ServiceProvider.GetRequiredService<IFilePathParserService>();
            var junitParser = scope.ServiceProvider.GetRequiredService<IJUnitParserService>();
            var testDataService = scope.ServiceProvider.GetRequiredService<ITestDataService>();

            const int batchSize = 100;
            for (int i = 0; i < xmlFiles.Length; i += batchSize)
            {
                var batch = xmlFiles.Skip(i).Take(batchSize);
                var allResults = new List<Models.TestResult>();

                foreach (var xmlFile in batch)
                {
                    try
                    {
                        // Parse file path
                        var parsedPath = filePathParser.ParseFilePath(xmlFile);
                        
                        // Log warning if using default values (both patterns failed)
                        if (parsedPath.DomainId == "Uncategorized")
                        {
                            _logger.LogWarning("Using default categorization for unrecognized path format: {Path}", xmlFile);
                        }

                        // Parse JUnit XML
                        var results = await junitParser.ParseJUnitXmlAsync(xmlFile, parsedPath);
                        allResults.AddRange(results);
                        fileCount++;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogWarning(ex, "Permission denied accessing file: {Path}. Skipping. Ensure service account has read permissions.", xmlFile);
                    }
                    catch (IOException ex) when (ex.Message.Contains("used by another process"))
                    {
                        _logger.LogWarning(ex, "File is locked by another process: {Path}. Will retry on next scan.", xmlFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing file: {Path}", xmlFile);
                    }
                }

                // Batch update test data service
                testDataService.AddOrUpdateTestResults(allResults);

                _logger.LogInformation("Processed batch {Current}/{Total} files", 
                    Math.Min(i + batchSize, xmlFiles.Length), xmlFiles.Length);
            }

            LastScanTime = DateTime.UtcNow;
            LastScanFileCount = fileCount;

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("File system scan completed. Processed {Count} files in {Duration:mm\\:ss}. Total test results: {Total}",
                fileCount, duration, testDataService.GetTotalCount());

            // Notify all connected clients that new data is available
            await _hubContext.Clients.All.SendAsync("TestDataUpdated", new
            {
                filesProcessed = fileCount,
                totalTestResults = testDataService.GetTotalCount(),
                scanTime = LastScanTime
            });
            _logger.LogInformation("Notified clients of test data update via SignalR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file system scan");
        }
        finally
        {
            IsScanningInProgress = false;
        }
    }

    /// <summary>
    /// Retry an operation with exponential backoff strategy for network failures
    /// </summary>
    private async Task<T> RetryWithBackoffAsync<T>(Func<Task<T>> operation, int maxRetries = 3, string operationName = "Operation")
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries - 1)
            {
                attempt++;
                var delayMs = (int)Math.Pow(2, attempt) * 1000; // Exponential backoff: 2s, 4s, 8s
                _logger.LogWarning(ex, "{OperationName} failed (attempt {Attempt}/{Max}), retrying in {DelayMs}ms", 
                    operationName, attempt, maxRetries, delayMs);
                await Task.Delay(delayMs);
            }
        }

        // Final attempt without catching
        return await operation();
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
