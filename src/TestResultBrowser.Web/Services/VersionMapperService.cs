using System.Text.RegularExpressions;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Implementation of version mapping service
/// Maps product version codes to semantic version numbers
/// </summary>
public partial class VersionMapperService : IVersionMapperService
{
    // Regex pattern to match PXrel{number} format
    [GeneratedRegex(@"^PXrel(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex PxrelPattern();

    /// <inheritdoc/>
    public string MapVersion(string rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            return "Unknown";
        }

        // Handle special case: "dev" -> "Development"
        if (rawVersion.Equals("dev", StringComparison.OrdinalIgnoreCase))
        {
            return "Development";
        }

        // Handle PXrel pattern: PXrel114 -> 1.14.0
        var match = PxrelPattern().Match(rawVersion);
        if (match.Success)
        {
            var versionNumber = int.Parse(match.Groups[1].Value);
            var major = versionNumber / 100;
            var minor = versionNumber % 100;
            return $"{major}.{minor}.0";
        }

        // Passthrough for unrecognized formats
        return rawVersion;
    }
}
