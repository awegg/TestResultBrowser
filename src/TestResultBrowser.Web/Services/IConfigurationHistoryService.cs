namespace TestResultBrowser.Web.Services;

using TestResultBrowser.Web.Models;

/// <summary>
/// Service for retrieving configuration history data for hierarchical browsing
/// </summary>
public interface IConfigurationHistoryService
{
    /// <summary>
    /// Get configuration history with hierarchical test tree and multi-build history
    /// </summary>
    /// <param name="configurationId">Configuration to retrieve (e.g., "1.14.0_Regular_Win2019SQLServer2022_CORE")</param>
    /// <param name="numberOfBuilds">Number of historical builds to include (default 5)</param>
    /// <returns>Configuration history result with tree and history columns</returns>
    Task<ConfigurationHistoryResult> GetConfigurationHistoryAsync(string configurationId, int numberOfBuilds = 5);

    /// <summary>
    /// Get list of all available configurations (for dropdown selection)
    /// </summary>
    /// <returns>List of unique configuration IDs</returns>
    Task<List<string>> GetAvailableConfigurationsAsync();

    /// <summary>
    /// Get list of builds (releases) available in the system
    /// </summary>
    /// <returns>Sorted list of build IDs</returns>
    Task<List<string>> GetAvailableBuildsAsync();

    /// <summary>
    /// Get list of configurations with their metadata including last update time
    /// </summary>
    /// <returns>List of configuration metadata with last update timestamp</returns>
    Task<List<ConfigurationMetadata>> GetConfigurationsWithMetadataAsync();
}
