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

    // Pattern 2 (Alternative): {Version}_{TestType}_{NamedConfig}_{Domain}\Release-{BuildNumber}\{Feature}\*.xml
    // Example: dev_E2E_Default1_Core\Release-193_176691\Px Core - Alarm Dashboard\tests-xxx.xml
    [GeneratedRegex(@"([^\\]+?)_([^\\]+?)_([^\\]+?)_([^\\]+?)\\Release-(\d+)(?:_\d+)?\\([^\\]+)\\(.+\.xml)$", RegexOptions.IgnoreCase)]
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
            var versionRaw = match2.Groups[1].Value;
            var testType = match2.Groups[2].Value; // E2E, etc.
            var namedConfig = match2.Groups[3].Value;
            var domainId = match2.Groups[4].Value;
            var buildNumber = int.Parse(match2.Groups[5].Value);
            var featureName = match2.Groups[6].Value; // "Px Core - Alarm Manager"
            var fileName = match2.Groups[7].Value;

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
