namespace TestResultBrowser.Web.Services;

/// <summary>
/// Service for mapping version strings to standardized version numbers
/// Maps build version codes (e.g., "PXrel114") to semantic versions (e.g., "1.14.0")
/// </summary>
public interface IVersionMapperService
{
    /// <summary>
    /// Maps a raw version string to a standardized version format
    /// </summary>
    /// <param name="rawVersion">Raw version string, e.g., "PXrel114", "dev"</param>
    /// <returns>Standardized version string, e.g., "1.14.0", "Development"</returns>
    /// <example>
    /// MapVersion("PXrel114") => "1.14.0"
    /// MapVersion("PXrel200") => "2.0.0"
    /// MapVersion("dev") => "Development"
    /// MapVersion("custom-1.0") => "custom-1.0" (passthrough for unrecognized formats)
    /// </example>
    string MapVersion(string rawVersion);
}
