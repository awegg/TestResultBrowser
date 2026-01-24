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

    public UserDataService(ILogger<UserDataService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _databasePath = configuration["TestResultBrowser:UserDataDatabasePath"] ?? "userdata.db";
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        EnsureIndexes();
        _logger.LogInformation("UserDataService initialized with database: {DatabasePath}", _databasePath);
    }

    /// <inheritdoc/>
    public Task<SavedFilterConfiguration> SaveFilterAsync(SavedFilterConfiguration filter)
    {
        try
        {
            using var db = new LiteDatabase(_databasePath);
            var collection = db.GetCollection<SavedFilterConfiguration>(FiltersCollection);
            
            filter.SavedDate = DateTime.UtcNow;
            collection.Insert(filter);
            
            _logger.LogInformation("Saved filter '{FilterName}' for user '{Username}' with ID {FilterId}", 
                filter.Name, filter.SavedBy, filter.Id);
            
            return Task.FromResult(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving filter '{FilterName}' for user '{Username}'", 
                filter.Name, filter.SavedBy);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<SavedFilterConfiguration?> LoadFilterAsync(int filterId)
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
            
            return Task.FromResult(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading filter ID {FilterId}", filterId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<List<SavedFilterConfiguration>> GetAllFiltersAsync(string username)
    {
        try
        {
            using var db = new LiteDatabase(_databasePath);
            var collection = db.GetCollection<SavedFilterConfiguration>(FiltersCollection);
            var filters = collection.Find(f => f.SavedBy == username).ToList();
            
            _logger.LogInformation("Retrieved {Count} filters for user '{Username}'", filters.Count, username);
            
            return Task.FromResult(filters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filters for user '{Username}'", username);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> DeleteFilterAsync(int filterId)
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
            
            return Task.FromResult(deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting filter ID {FilterId}", filterId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> UpdateFilterAsync(SavedFilterConfiguration filter)
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
            
            return Task.FromResult(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating filter ID {FilterId}", filter.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<DashboardConfiguration?> GetDashboardConfigAsync(string username)
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
            
            return Task.FromResult(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard config for user '{Username}'", username);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<DashboardConfiguration> SaveDashboardConfigAsync(DashboardConfiguration config)
    {
        try
        {
            using var db = new LiteDatabase(_databasePath);
            var collection = db.GetCollection<DashboardConfiguration>(DashboardCollection);
            
            config.LastUpdated = DateTime.UtcNow;
            
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
            
            return Task.FromResult(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving dashboard config for user '{Username}'", config.Username);
            throw;
        }
    }

    private void EnsureIndexes()
    {
        try
        {
            using var db = new LiteDatabase(_databasePath);
            var filters = db.GetCollection<SavedFilterConfiguration>(FiltersCollection);
            filters.EnsureIndex(f => f.SavedBy);

            var dashboards = db.GetCollection<DashboardConfiguration>(DashboardCollection);
            dashboards.EnsureIndex(d => d.Username);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error ensuring LiteDB indexes (continuing without indexes)");
        }
    }
}
