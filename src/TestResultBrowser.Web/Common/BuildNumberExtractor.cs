namespace TestResultBrowser.Web.Common;

/// <summary>
/// Utility class for extracting build numbers from build IDs
/// Shared across services to eliminate duplication
/// </summary>
public static class BuildNumberExtractor
{
    /// <summary>
    /// Extracts numeric build number from build ID string
    /// </summary>
    /// <param name="buildId">Build ID in format "Release-{number}" or similar</param>
    /// <returns>Numeric build number, or 0 if extraction fails</returns>
    public static int ExtractBuildNumber(string buildId)
    {
        if (string.IsNullOrEmpty(buildId))
            return 0;

        // Try to extract number after last hyphen or underscore
        var parts = buildId.Split('-', '_');
        if (parts.Length > 0)
        {
            var lastPart = parts[^1];
            if (int.TryParse(lastPart, out var buildNumber))
            {
                return buildNumber;
            }
        }

        return 0;
    }

    /// <summary>
    /// Extracts build time from build ID string
    /// </summary>
    /// <param name="buildId">Build ID containing timestamp</param>
    /// <returns>DateTime or DateTime.MinValue if extraction fails</returns>
    public static DateTime GetBuildTime(string buildId)
    {
        // For now, use build number as proxy for time
        // In real implementation, could parse timestamp from directory
        var buildNumber = ExtractBuildNumber(buildId);
        if (buildNumber > 0)
        {
            // Use a base date + build number days as approximation
            return new DateTime(2024, 1, 1).AddDays(buildNumber);
        }
        
        return DateTime.MinValue;
    }
}
