using System.Text.RegularExpressions;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Implementation of file path parser service
/// Extracts metadata from test result file paths
/// Supports two formats:
/// 1. Release-{BuildNumber}\{Version}_{TestType}_{NamedConfig}_{Domain}\*.xml
/// 2. {Version}_{NamedConfig}_{Domain}\Release-{BuildNumber}\{Feature}\*.xml
/// </summary>
public partial class FilePathParserService : IFilePathParserService
{
    // Pattern 1 (Preferred): Release-{BuildNumber}\{Version}_{TestType}_{NamedConfig}_{Domain}\*.xml
    [GeneratedRegex(@"Release-(\d+)\\([^_]+)_([^_]+)_([^_]+)_([^\\]+)\\(.+\.xml)$", RegexOptions.IgnoreCase)]
    private static partial Regex FilePathPattern1();

    // Pattern 2 (Alternative): {Config}\Release-{BuildNumber}\{Feature}\*.xml
    // Example: dev_E2E_Default1_Core\Release-227_180127\Px Core - Alarm Manager\tests-xxx.xml
    // Captures entire config as single group (allows spaces and underscores in feature names)
    [GeneratedRegex(@"([^\\]+)\\Release-(\d+)(?:_\d+)?\\([^\\]+)\\(.+\.xml)$", RegexOptions.IgnoreCase)]
    private static partial Regex FilePathPattern2();

    /// <inheritdoc/>
    public ParsedFilePath ParseFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return CreateDefaultParsedPath(filePath ?? string.Empty);
        }

        // Normalize path separators for cross-platform compatibility
        var normalizedPath = filePath.Replace('/', '\\');

        // Try Pattern 1 first (preferred format)
        var match1 = FilePathPattern1().Match(filePath);
        if (match1.Success)
        {
            var buildNumber = int.Parse(match1.Groups[1].Value);
            var versionRaw = match1.Groups[2].Value;
            var testType = match1.Groups[3].Value;
            var namedConfig = match1.Groups[4].Value;
            var domainId = match1.Groups[5].Value;
            var fileName = match1.Groups[6].Value;

            return new ParsedFilePath
            {
                BuildNumber = buildNumber,
                BuildId = $"Release-{buildNumber}",
                VersionRaw = versionRaw,
                TestType = testType,
                NamedConfig = namedConfig,
                DomainId = domainId,
                FilePath = filePath,
                FileName = fileName
            };
        }

        // Try Pattern 2 (alternative format)
        var match2 = FilePathPattern2().Match(normalizedPath);
        if (match2.Success)
        {
            var configRaw = match2.Groups[1].Value;  // e.g., "dev_E2E_Default1_Core"
            var buildNumber = int.Parse(match2.Groups[2].Value);
            var featureName = match2.Groups[3].Value; // "Px Core - Alarm Manager"
            var fileName = match2.Groups[4].Value;

            // Parse the config string to extract components
            // Expected format: {Version}_{TestType}_{NamedConfig}_{Domain}
            // Example: dev_E2E_Default1_Core -> Version=dev, TestType=E2E, NamedConfig=Default1, Domain=Core
            var (versionRaw, testType, namedConfig, domainId) = ParseConfigParts(configRaw);

            return new ParsedFilePath
            {
                BuildNumber = buildNumber,
                BuildId = $"Release-{buildNumber}",
                VersionRaw = versionRaw,
                TestType = testType,
                NamedConfig = namedConfig,
                DomainId = domainId,
                FilePath = filePath,
                FileName = fileName
            };
        }

        // Both patterns failed - return default values so file is still processed
        if (TryParseFallback(normalizedPath, filePath, out var fallbackParsed))
        {
            return fallbackParsed;
        }

        return CreateDefaultParsedPath(filePath);
    }

    private static bool TryParseFallback(string normalizedPath, string originalFilePath, out ParsedFilePath parsed)
    {
        parsed = default!;

        var segments = normalizedPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return false;
        }

        var fileName = Path.GetFileName(normalizedPath);
        var buildNumber = 0;
        var buildId = "Unknown";

        var releaseIndex = Array.FindIndex(segments, s => s.StartsWith("Release-", StringComparison.OrdinalIgnoreCase));
        if (releaseIndex >= 0)
        {
            var match = Regex.Match(segments[releaseIndex], @"^Release-(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var parsedBuild))
            {
                buildNumber = parsedBuild;
                buildId = $"Release-{parsedBuild}";
            }
        }

        string? configRaw = null;
        if (releaseIndex >= 0)
        {
            if (releaseIndex + 1 < segments.Length && segments[releaseIndex + 1].Contains('_'))
            {
                configRaw = segments[releaseIndex + 1];
            }
            else if (releaseIndex - 1 >= 0 && segments[releaseIndex - 1].Contains('_'))
            {
                configRaw = segments[releaseIndex - 1];
            }
        }

        if (string.IsNullOrWhiteSpace(configRaw))
        {
            configRaw = segments.FirstOrDefault(s => s.Contains('_') && !s.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        }

        if (string.IsNullOrWhiteSpace(configRaw))
        {
            return false;
        }

        var (versionRaw, testType, namedConfig, domainId) = ParseConfigParts(configRaw);

        parsed = new ParsedFilePath
        {
            BuildNumber = buildNumber,
            BuildId = buildId,
            VersionRaw = versionRaw,
            TestType = testType,
            NamedConfig = namedConfig,
            DomainId = domainId,
            FilePath = originalFilePath,
            FileName = string.IsNullOrWhiteSpace(fileName) ? "Unknown.xml" : fileName
        };

        return true;
    }

    private static (string VersionRaw, string TestType, string NamedConfig, string DomainId) ParseConfigParts(string configRaw)
    {
        var parts = configRaw.Split('_', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 4)
        {
            return (parts[0], parts[1], parts[2], string.Join("_", parts.Skip(3)));
        }

        if (parts.Length == 3)
        {
            return (parts[0], parts[1], parts[2], "Unknown");
        }

        if (parts.Length == 2)
        {
            return (parts[0], "Unknown", parts[1], "Unknown");
        }

        if (parts.Length == 1)
        {
            return (parts[0], "Unknown", "Default", "Unknown");
        }

        return ("Unknown", "Unknown", "Default", "Uncategorized");
    }

    private static ParsedFilePath CreateDefaultParsedPath(string filePath)
    {
        var fileNameOnly = string.IsNullOrWhiteSpace(filePath) ? "Unknown.xml" : Path.GetFileName(filePath);

        return new ParsedFilePath
        {
            BuildNumber = 0,
            BuildId = "Unknown",
            VersionRaw = "Unknown",
            TestType = "Unknown",
            NamedConfig = "Default",
            DomainId = "Uncategorized",
            FilePath = filePath ?? string.Empty,
            FileName = fileNameOnly
        };
    }
}
