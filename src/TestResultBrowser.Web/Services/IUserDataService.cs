using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Service for managing user data and preferences stored in LiteDB
/// </summary>
public interface IUserDataService
{
    /// <summary>
    /// Save a filter configuration
    /// </summary>
    Task<SavedFilterConfiguration> SaveFilterAsync(SavedFilterConfiguration filter);

    /// <summary>
    /// Load a filter configuration by ID
    /// </summary>
    Task<SavedFilterConfiguration?> LoadFilterAsync(int filterId);

    /// <summary>
    /// Get all saved filters for a user
    /// </summary>
    Task<List<SavedFilterConfiguration>> GetAllFiltersAsync(string username);

    /// <summary>
    /// Delete a filter configuration
    /// </summary>
    Task<bool> DeleteFilterAsync(int filterId);

    /// <summary>
    /// Update an existing filter configuration
    /// </summary>
    Task<bool> UpdateFilterAsync(SavedFilterConfiguration filter);

    /// <summary>
    /// Get dashboard configuration for a user
    /// </summary>
    Task<DashboardConfiguration?> GetDashboardConfigAsync(string username);

    /// <summary>
    /// Save or update dashboard configuration
    /// </summary>
    Task<DashboardConfiguration> SaveDashboardConfigAsync(DashboardConfiguration config);
}
