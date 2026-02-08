using Microsoft.Extensions.Options;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Converts report directory paths into static file URLs under /test-reports.
/// </summary>
public class TestReportUrlService : ITestReportUrlService
{
    private readonly string _baseDirectory;

    public TestReportUrlService(IOptions<TestResultBrowserOptions> options)
    {
        _baseDirectory = string.IsNullOrWhiteSpace(options?.Value.FileSharePath)
            ? string.Empty
            : Path.GetFullPath(options.Value.FileSharePath);
    }

    /// <inheritdoc/>
    public string? GetReportUrl(string? reportDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(reportDirectoryPath))
        {
            return null;
        }

        var normalizedPath = reportDirectoryPath.Trim();

        if (string.IsNullOrWhiteSpace(_baseDirectory))
        {
            return null;
        }

        if (Path.IsPathRooted(normalizedPath))
        {
            var fullPath = Path.GetFullPath(normalizedPath);
            if (!IsUnderBaseDirectory(fullPath))
            {
                return null;
            }

            normalizedPath = Path.GetRelativePath(_baseDirectory, fullPath);
        }
        else
        {
            var fullPath = Path.GetFullPath(Path.Combine(_baseDirectory, normalizedPath));
            if (!IsUnderBaseDirectory(fullPath))
            {
                return null;
            }

            normalizedPath = Path.GetRelativePath(_baseDirectory, fullPath);
        }

        normalizedPath = normalizedPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var urlPath = normalizedPath
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');

        var segments = urlPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var encodedPath = string.Join('/', segments.Select(Uri.EscapeDataString));

        return string.IsNullOrEmpty(encodedPath) ? null : $"/test-reports/{encodedPath}";
    }

    private bool IsUnderBaseDirectory(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(_baseDirectory))
        {
            return false;
        }

        var baseDir = _baseDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(fullPath, _baseDirectory, StringComparison.OrdinalIgnoreCase);
    }
}
