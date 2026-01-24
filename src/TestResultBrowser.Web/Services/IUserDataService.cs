using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Service for managing user data and preferences stored in LiteDB
/// </summary>
public interface IUserDataService
{
    /// <summary>
    /// Save a filter configuration
    /// <summary>
/// Saves the provided filter configuration to persistent storage and returns the stored instance.
/// </summary>
/// <param name="filter">The filter configuration to save; may be updated with persisted fields (for example, an assigned Id).</param>
/// <returns>The saved <see cref="SavedFilterConfiguration"/> including any fields populated during persistence.</returns>
    Task<SavedFilterConfiguration> SaveFilterAsync(SavedFilterConfiguration filter);

    /// <summary>
    /// Load a filter configuration by ID
    /// <summary>
/// Loads a saved filter configuration by its identifier.
/// </summary>
/// <param name="filterId">The identifier of the saved filter to load.</param>
/// <returns>The saved <see cref="SavedFilterConfiguration"/> with the specified identifier, or <c>null</c> if not found.</returns>
    Task<SavedFilterConfiguration?> LoadFilterAsync(int filterId);

    /// <summary>
    /// Get all saved filters for a user
    /// <summary>
/// Retrieves all saved filter configurations for the specified user.
/// </summary>
/// <param name="username">The username whose saved filters to retrieve.</param>
/// <returns>A list of SavedFilterConfiguration objects belonging to the user; an empty list if the user has no saved filters.</returns>
    Task<List<SavedFilterConfiguration>> GetAllFiltersAsync(string username);

    /// <summary>
    /// Delete a filter configuration
    /// <summary>
/// Deletes a saved filter configuration by its identifier.
/// </summary>
/// <param name="filterId">The identifier of the saved filter to delete.</param>
/// <returns>`true` if a filter with the specified ID was found and deleted, `false` otherwise.</returns>
    Task<bool> DeleteFilterAsync(int filterId);

    /// <summary>
    /// Update an existing filter configuration
    /// <summary>
/// Updates an existing saved filter configuration.
/// </summary>
/// <param name="filter">The filter configuration containing updated values; its identifier determines which saved filter to update.</param>
/// <returns>`true` if the filter was successfully updated, `false` otherwise.</returns>
    Task<bool> UpdateFilterAsync(SavedFilterConfiguration filter);

    /// <summary>
    /// Get dashboard configuration for a user
    /// <summary>
/// Retrieves the dashboard configuration for the specified user.
/// </summary>
/// <param name="username">The username whose dashboard configuration to retrieve.</param>
/// <returns>The <see cref="DashboardConfiguration"/> for the user, or <c>null</c> if none exists.</returns>
    Task<DashboardConfiguration?> GetDashboardConfigAsync(string username);

    /// <summary>
    /// Save or update dashboard configuration
    /// <summary>
/// Saves or updates a user's dashboard configuration.
/// </summary>
/// <param name="config">The dashboard configuration to save or update.</param>
/// <returns>The persisted DashboardConfiguration reflecting any changes made during save or update.</returns>
    Task<DashboardConfiguration> SaveDashboardConfigAsync(DashboardConfiguration config);
}