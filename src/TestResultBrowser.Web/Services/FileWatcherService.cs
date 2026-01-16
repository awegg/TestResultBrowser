using Microsoft.Extensions.Options;

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
    private Timer? _timer;

    public DateTime? LastScanTime { get; private set; }
    public int LastScanFileCount { get; private set; }
    public bool IsScanningInProgress { get; private set; }

    public FileWatcherService(
        ILogger<FileWatcherService> logger,
        IServiceProvider serviceProvider,
        IOptions<TestResultBrowserOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
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
            _logger.LogWarning("Scan already in progress, skipping");
            return;
        }

        IsScanningInProgress = true;
        var startTime = DateTime.UtcNow;
        var fileCount = 0;

        try
        {
            _logger.LogInformation("Starting file system scan of {Path}", _options.FileSharePath);

            if (!Directory.Exists(_options.FileSharePath))
            {
                _logger.LogError("File share path does not exist: {Path}", _options.FileSharePath);
                return;
            }

            // Find all XML files matching pattern: Release-*/*/*.xml
            var xmlFiles = Directory.GetFiles(
                _options.FileSharePath,
                "*.xml",
                SearchOption.AllDirectories);

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
                        if (parsedPath == null)
                        {
                            _logger.LogWarning("Could not parse file path: {Path}", xmlFile);
                            continue;
                        }

                        // Parse JUnit XML
                        var results = await junitParser.ParseJUnitXmlAsync(xmlFile, parsedPath);
                        allResults.AddRange(results);
                        fileCount++;
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

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
