using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Service for triage operations - comparing test runs and identifying changes
/// </summary>
public interface ITriageService
{
    /// <summary>
    /// Performs morning triage analysis comparing most recent two builds
    /// </summary>
    /// <param name="selectedDomains">Optional domain filter (null = all domains)</param>
    /// <returns>Triage result with new failures, fixed tests, and metrics</returns>
    Task<MorningTriageResult?> GetMorningTriageAsync(List<string>? selectedDomains = null);
    
    /// <summary>
    /// Performs morning triage analysis for specific builds
    /// </summary>
    /// <param name="todayBuildId">Build ID for today/current run</param>
    /// <param name="yesterdayBuildId">Build ID for yesterday/previous run</param>
    /// <param name="selectedDomains">Optional domain filter</param>
    /// <returns>Triage result comparing the two builds</returns>
    Task<MorningTriageResult?> GetMorningTriageAsync(
        string todayBuildId, 
        string yesterdayBuildId, 
        List<string>? selectedDomains = null);

    /// <summary>
    /// Performs release triage for a specific release build, optionally compared to a previous candidate
    /// </summary>
    /// <param name="releaseBuildId">Current release build ID</param>
    /// <param name="previousReleaseBuildId">Optional previous release build ID for comparison</param>
    /// <returns>Release triage result including configuration matrix and summaries</returns>
    Task<ReleaseTriageResult?> GetReleaseTriageAsync(string releaseBuildId, string? previousReleaseBuildId = null);
}
