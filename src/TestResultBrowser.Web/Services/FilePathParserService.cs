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
        var match2 = FilePathPattern2().Match(filePath);
        if (match2.Success)
        {
            var configRaw = match2.Groups[1].Value;  // e.g., "dev_E2E_Default1_Core"
            var buildNumber = int.Parse(match2.Groups[2].Value);
            var featureName = match2.Groups[3].Value; // "Px Core - Alarm Manager"
            var fileName = match2.Groups[4].Value;

            // Parse the config string to extract components
            // Expected format: {Version}_{TestType}_{NamedConfig}_{Domain}
            // Example: dev_E2E_Default1_Core -> Version=dev, TestType=E2E, NamedConfig=Default1, Domain=Core
            var configParts = configRaw.Split('_');
            
            string versionRaw = "Unknown";
            string testType = "Unknown";
            string namedConfig = "Unknown";
            string domainId = "Unknown";
            
            if (configParts.Length >= 4)
            {
                versionRaw = configParts[0];
                testType = configParts[1];
                namedConfig = configParts[2];
                domainId = string.Join("_", configParts.Skip(3)); // Handle multi-part domains like "TnT_Prod"
            }

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
        return CreateDefaultParsedPath(filePath);
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
