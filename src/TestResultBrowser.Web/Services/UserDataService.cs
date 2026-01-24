using LiteDB;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Service for managing user data and preferences using LiteDB
/// </summary>
public class UserDataService : IUserDataService
{
    private readonly ILogger<UserDataService> _logger;
    private readonly string _databasePath;
    private const string FiltersCollection = "savedfilters";
    private const string DashboardCollection = "dashboards";

    /// <summary>
    /// Initializes a new instance of <see cref="UserDataService"/>, configures the database file path, ensures its directory exists, and logs initialization.
    /// </summary>
    /// <remarks>
    /// Reads the configuration key "UserDataDatabasePath" to determine the database path; uses "userdata.db" if the key is not present. Creates the directory for the database file when necessary and logs the selected path.
    /// </remarks>
    public UserDataService(ILogger<UserDataService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _databasePath = configuration["UserDataDatabasePath"] ?? "userdata.db";
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _logger.LogInformation("UserDataService initialized with database: {DatabasePath}", _databasePath);
    }

    /// <summary>
    /// Persists the provided SavedFilterConfiguration to the user data database and updates its saved timestamp.
    /// </summary>
    /// <param name="filter">The filter to save; its <see cref="SavedFilterConfiguration.SavedDate"/> will be set to UTC now and an Id will be assigned on insert.</param>
    /// <returns>The saved <see cref="SavedFilterConfiguration"/> instance with an updated SavedDate and assigned Id.</returns>
    public async Task<SavedFilterConfiguration> SaveFilterAsync(SavedFilterConfiguration filter)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var db = new LiteDatabase(_databasePath);
                var collection = db.GetCollection<SavedFilterConfiguration>(FiltersCollection);
                
                filter.SavedDate = DateTime.UtcNow;
                collection.Insert(filter);
                
                _logger.LogInformation("Saved filter '{FilterName}' for user '{Username}' with ID {FilterId}", 
                    filter.Name, filter.SavedBy, filter.Id);
                
                return filter;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving filter '{FilterName}' for user '{Username}'", 
                    filter.Name, filter.SavedBy);
                throw;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<SavedFilterConfiguration?> LoadFilterAsync(int filterId)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var db = new LiteDatabase(_databasePath);
                var collection = db.GetCollection<SavedFilterConfiguration>(FiltersCollection);
                var filter = collection.FindById(filterId);
                
                if (filter != null)
                {
                    _logger.LogInformation("Loaded filter ID {FilterId}: '{FilterName}'", filterId, filter.Name);
                }
                else
                {
                    _logger.LogWarning("Filter ID {FilterId} not found", filterId);
                }
                
                return filter;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading filter ID {FilterId}", filterId);
                throw;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<List<SavedFilterConfiguration>> GetAllFiltersAsync(string username)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var db = new LiteDatabase(_databasePath);
                var collection = db.GetCollection<SavedFilterConfiguration>(FiltersCollection);
                var filters = collection.Find(f => f.SavedBy == username).ToList();
                
                _logger.LogInformation("Retrieved {Count} filters for user '{Username}'", filters.Count, username);
                
                return filters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filters for user '{Username}'", username);
                throw;
            }
        });
    }

    /// <summary>
    /// Deletes a saved filter with the specified identifier.
    /// </summary>
    /// <returns>`true` if the filter was deleted, `false` if no filter with the given ID was found.</returns>
    public async Task<bool> DeleteFilterAsync(int filterId)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var db = new LiteDatabase(_databasePath);
                var collection = db.GetCollection<SavedFilterConfiguration>(FiltersCollection);
                var deleted = collection.Delete(filterId);
                
                if (deleted)
                {
                    _logger.LogInformation("Deleted filter ID {FilterId}", filterId);
                }
                else
                {
                    _logger.LogWarning("Filter ID {FilterId} not found for deletion", filterId);
                }
                
                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting filter ID {FilterId}", filterId);
                throw;
            }
        });
    }

    /// <summary>
    /// Updates an existing saved filter record in the user data store.
    /// </summary>
    /// <param name="filter">The filter to update; the filter's Id is used to locate the existing record.</param>
    /// <returns>`true` if the filter was updated, `false` if no matching record was found.</returns>
    public async Task<bool> UpdateFilterAsync(SavedFilterConfiguration filter)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var db = new LiteDatabase(_databasePath);
                var collection = db.GetCollection<SavedFilterConfiguration>(FiltersCollection);
                
                filter.SavedDate = DateTime.UtcNow;
                var updated = collection.Update(filter);
                
                if (updated)
                {
                    _logger.LogInformation("Updated filter ID {FilterId}: '{FilterName}'", filter.Id, filter.Name);
                }
                else
                {
                    _logger.LogWarning("Filter ID {FilterId} not found for update", filter.Id);
                }
                
                return updated;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating filter ID {FilterId}", filter.Id);
                throw;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<DashboardConfiguration?> GetDashboardConfigAsync(string username)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var db = new LiteDatabase(_databasePath);
                var collection = db.GetCollection<DashboardConfiguration>(DashboardCollection);
                var config = collection.FindOne(d => d.Username == username);
                
                if (config != null)
                {
                    _logger.LogInformation("Loaded dashboard config for user '{Username}'", username);
                }
                
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard config for user '{Username}'", username);
                throw;
            }
        });
    }

    /// <summary>
    /// Creates or updates the dashboard configuration for the specified user and sets its LastUpdated timestamp to the current UTC time.
    /// </summary>
    /// <param name="config">The dashboard configuration to create or update; its Username identifies the owner. On update the existing Id is preserved.</param>
    /// <returns>The saved DashboardConfiguration with an assigned Id and updated LastUpdated timestamp.</returns>
    public async Task<DashboardConfiguration> SaveDashboardConfigAsync(DashboardConfiguration config)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var db = new LiteDatabase(_databasePath);
                var collection = db.GetCollection<DashboardConfiguration>(DashboardCollection);
                
                config.LastUpdated = DateTime.UtcNow;
                
                // Check if config exists for this user
                var existing = collection.FindOne(d => d.Username == config.Username);
                if (existing != null)
                {
                    config.Id = existing.Id;
                    collection.Update(config);
                    _logger.LogInformation("Updated dashboard config for user '{Username}'", config.Username);
                }
                else
                {
                    collection.Insert(config);
                    _logger.LogInformation("Created dashboard config for user '{Username}'", config.Username);
                }
                
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving dashboard config for user '{Username}'", config.Username);
                throw;
            }
        });
    }
}