using LiteDB;
using Microsoft.Extensions.Options;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Service for managing user-configurable application settings stored in LiteDB
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _userDataPath;
    private readonly ILogger<SettingsService> _logger;
    private readonly TestResultBrowserOptions _options;
    private ApplicationSettings? _cachedSettings;
    private readonly object _lock = new();

    public event EventHandler? SettingsChanged;

    public SettingsService(
        IOptions<TestResultBrowserOptions> options,
        ILogger<SettingsService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Get userdata path from configuration or use default
        _userDataPath = !string.IsNullOrEmpty(_options.UserDataPath)
            ? _options.UserDataPath
            : Path.Combine(Directory.GetCurrentDirectory(), "userdata");

        // Ensure directory exists
        Directory.CreateDirectory(_userDataPath);

        _logger.LogInformation("Settings service initialized with userdata path: {UserDataPath}", _userDataPath);
    }

    /// <inheritdoc/>
    public ApplicationSettings GetSettings()
    {
        lock (_lock)
        {
            // Always reload from database to ensure fresh data
            var dbPath = Path.Combine(_userDataPath, "settings.db");

            try
            {
                using var db = new LiteDatabase(dbPath);
                var collection = db.GetCollection<ApplicationSettings>("settings");

                var settings = collection.FindById("default");

                if (settings == null)
                {
                    // No saved settings, create defaults with values from appsettings.json/env vars
                    settings = new ApplicationSettings
                    {
                        Id = "default",
                        PollingIntervalMinutes = _options.PollingIntervalMinutes,
                        PolarionBaseUrl = _options.PolarionBaseUrl,
                        MaxMemoryGB = _options.MaxMemoryGB,
                        FlakyTestThresholds = new FlakyTestThresholds
                        {
                            RollingWindowSize = _options.FlakyTestThresholds.RollingWindowSize,
                            TriggerPercentage = _options.FlakyTestThresholds.TriggerPercentage,
                            ClearAfterConsecutivePasses = _options.FlakyTestThresholds.ClearAfterConsecutivePasses
                        }
                    };

                    // Save defaults to database
                    collection.Upsert(settings);
                    _logger.LogInformation("Created default settings from configuration");
                }

                _cachedSettings = settings;
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings from database, using defaults");
                return new ApplicationSettings
                {
                    Id = "default",
                    PollingIntervalMinutes = _options.PollingIntervalMinutes,
                    PolarionBaseUrl = _options.PolarionBaseUrl,
                    MaxMemoryGB = _options.MaxMemoryGB,
                    FlakyTestThresholds = new FlakyTestThresholds
                    {
                        RollingWindowSize = _options.FlakyTestThresholds.RollingWindowSize,
                        TriggerPercentage = _options.FlakyTestThresholds.TriggerPercentage,
                        ClearAfterConsecutivePasses = _options.FlakyTestThresholds.ClearAfterConsecutivePasses
                    }
                };
            }
        }
    }

    /// <inheritdoc/>
    public Task SaveSettingsAsync(ApplicationSettings settings)
    {
        lock (_lock)
        {
            var dbPath = Path.Combine(_userDataPath, "settings.db");

            try
            {
                settings.Id = "default"; // Ensure consistent ID

                using (var db = new LiteDatabase(dbPath))
                {
                    var collection = db.GetCollection<ApplicationSettings>("settings");

                    // Delete existing record to ensure clean upsert
                    collection.DeleteMany(x => x.Id == "default");

                    // Insert fresh record
                    collection.Insert(settings);


                }

                // Update cache after successful save
                _cachedSettings = settings;
                _logger.LogInformation("Settings persisted to {DbPath}", dbPath);

                // Raise event
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings to database at {DbPath}", dbPath);
                throw;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResetToDefaultsAsync()
    {
        var defaults = new ApplicationSettings
        {
            Id = "default",
            PollingIntervalMinutes = _options.PollingIntervalMinutes,
            PolarionBaseUrl = _options.PolarionBaseUrl,
            MaxMemoryGB = _options.MaxMemoryGB,
            FlakyTestThresholds = new FlakyTestThresholds
            {
                RollingWindowSize = _options.FlakyTestThresholds.RollingWindowSize,
                TriggerPercentage = _options.FlakyTestThresholds.TriggerPercentage,
                ClearAfterConsecutivePasses = _options.FlakyTestThresholds.ClearAfterConsecutivePasses
            }
        };

        return SaveSettingsAsync(defaults);
    }
}
