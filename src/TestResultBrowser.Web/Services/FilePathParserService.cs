using System.Text.RegularExpressions;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Implementation of file path parser service
/// Extracts metadata from test result file paths
/// </summary>
public partial class FilePathParserService : IFilePathParserService
{
    // Pattern: Release-{BuildNumber}\{Version}_{TestType}_{NamedConfig}_{Domain}\*.xml
    [GeneratedRegex(@"Release-(\d+)\\([^_]+)_([^_]+)_([^_]+)_([^\\]+)\\(.+\.xml)$", RegexOptions.IgnoreCase)]
    private static partial Regex FilePathPattern();

    /// <inheritdoc/>
    public ParsedFilePath? ParseFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        var match = FilePathPattern().Match(filePath);
        if (!match.Success)
        {
            return null;
        }

        var buildNumber = int.Parse(match.Groups[1].Value);
        var versionRaw = match.Groups[2].Value;
        var testType = match.Groups[3].Value;
        var namedConfig = match.Groups[4].Value;
        var domainId = match.Groups[5].Value;
        var fileName = match.Groups[6].Value;

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
}
