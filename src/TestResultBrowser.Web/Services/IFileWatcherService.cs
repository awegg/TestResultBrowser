namespace TestResultBrowser.Web.Services;

/// <summary>
/// Service for monitoring file system and automatically importing new test results
/// Runs as a background service with configurable polling interval
/// </summary>
public interface IFileWatcherService
{
    /// <summary>
    /// Gets the last scan timestamp
    /// </summary>
    DateTime? LastScanTime { get; }

    /// <summary>
    /// Gets the count of files processed in last scan
    /// </summary>
    int LastScanFileCount { get; }

    /// <summary>
    /// Gets whether a scan is currently in progress
    /// </summary>
    bool IsScanningInProgress { get; }

    /// <summary>
    /// Manually triggers an immediate scan
    /// </summary>
    Task ScanNowAsync();
}
