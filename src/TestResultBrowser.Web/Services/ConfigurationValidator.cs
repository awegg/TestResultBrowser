using Microsoft.Extensions.Options;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Startup configuration validator
/// Validates file paths, URLs, and threshold values before application starts
/// </summary>
public class ConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;
    private readonly TestResultBrowserOptions _options;

    public ConfigurationValidator(
        ILogger<ConfigurationValidator> logger,
        IOptions<TestResultBrowserOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Validates configuration and logs warnings for any issues
    /// </summary>
    /// <returns>True if configuration is valid, false otherwise</returns>
    public bool ValidateConfiguration()
    {
        var isValid = true;

        // Validate FileSharePath
        if (string.IsNullOrWhiteSpace(_options.FileSharePath))
        {
            _logger.LogError("FileSharePath is not configured");
            isValid = false;
        }
        else if (!Directory.Exists(_options.FileSharePath))
        {
            _logger.LogWarning("FileSharePath does not exist: {Path}. Will be created if accessible.", _options.FileSharePath);
        }

        // Validate PollingIntervalMinutes
        if (_options.PollingIntervalMinutes < 1 || _options.PollingIntervalMinutes > 1440)
        {
            _logger.LogWarning("PollingIntervalMinutes should be between 1 and 1440 (24 hours). Current value: {Value}", _options.PollingIntervalMinutes);
        }

        // Validate FlakyTestThresholds
        var flaky = _options.FlakyTestThresholds;
        if (flaky.RollingWindowSize < 5 || flaky.RollingWindowSize > 100)
        {
            _logger.LogWarning("FlakyTestThresholds.RollingWindowSize should be between 5 and 100. Current value: {Value}", flaky.RollingWindowSize);
        }

        if (flaky.FlakinessTriggerPercentage < 1 || flaky.FlakinessTriggerPercentage > 100)
        {
            _logger.LogError("FlakyTestThresholds.FlakinessTriggerPercentage must be between 1 and 100. Current value: {Value}", flaky.FlakinessTriggerPercentage);
            isValid = false;
        }

        if (flaky.ClearAfterConsecutivePasses < 1 || flaky.ClearAfterConsecutivePasses > 50)
        {
            _logger.LogWarning("FlakyTestThresholds.ClearAfterConsecutivePasses should be between 1 and 50. Current value: {Value}", flaky.ClearAfterConsecutivePasses);
        }

        // Validate WorkItemBaseUrl (if configured)
        if (!string.IsNullOrWhiteSpace(_options.WorkItemBaseUrl))
        {
            if (!Uri.TryCreate(_options.WorkItemBaseUrl, UriKind.Absolute, out var uri))
            {
                _logger.LogWarning("WorkItemBaseUrl is not a valid URL: {Url}", _options.WorkItemBaseUrl);
            }
            else if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                _logger.LogWarning("WorkItemBaseUrl should use http or https scheme: {Url}", _options.WorkItemBaseUrl);
            }
        }

        // Validate Cache settings
        var cache = _options.Cache;
        if (cache.MaxMemoryGB < 1 || cache.MaxMemoryGB > 64)
        {
            _logger.LogWarning("Cache.MaxMemoryGB should be between 1 and 64. Current value: {Value}", cache.MaxMemoryGB);
        }

        if (cache.AggregationCacheMinutes < 1 || cache.AggregationCacheMinutes > 60)
        {
            _logger.LogWarning("Cache.AggregationCacheMinutes should be between 1 and 60. Current value: {Value}", cache.AggregationCacheMinutes);
        }

        if (isValid)
        {
            _logger.LogInformation("Configuration validation passed");
        }
        else
        {
            _logger.LogError("Configuration validation failed. Please check appsettings.json");
        }

        return isValid;
    }
}
