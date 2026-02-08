using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Extracts report assets (screenshots/videos) from report.json on-demand.
/// </summary>
public class ReportAssetService : IReportAssetService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private readonly ITestReportUrlService _urlService;
    private readonly string _baseDirectory;
    private readonly ILogger<ReportAssetService> _logger;
    private readonly IMemoryCache _cache;

    public ReportAssetService(
        ITestReportUrlService urlService,
        IOptions<TestResultBrowserOptions> options,
        ILogger<ReportAssetService> logger,
        IMemoryCache cache)
    {
        _urlService = urlService;
        _logger = logger;
        _cache = cache;
        _baseDirectory = string.IsNullOrWhiteSpace(options?.Value.FileSharePath)
            ? string.Empty
            : Path.GetFullPath(options.Value.FileSharePath);
    }

    public async Task<ReportAssetInfo?> GetAssetsAsync(TestResult testResult)
    {
        if (testResult == null)
        {
            return null;
        }

        if (_cache.TryGetValue(testResult.Id, out ReportAssetInfo? cached))
        {
            return cached;
        }

        var reportDirectory = ResolveReportDirectory(testResult.ReportDirectoryPath);
        if (string.IsNullOrEmpty(reportDirectory))
        {
            _cache.Set<ReportAssetInfo?>(testResult.Id, null, CacheDuration);
            return null;
        }

        var reportJsonPath = Path.Combine(reportDirectory, "report.json");
        if (!File.Exists(reportJsonPath))
        {
            _cache.Set<ReportAssetInfo?>(testResult.Id, null, CacheDuration);
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(reportJsonPath);
            using var doc = await JsonDocument.ParseAsync(stream);

            var assets = FindAssets(doc.RootElement, testResult.TestFullName);
            if (assets == null)
            {
                _cache.Set<ReportAssetInfo?>(testResult.Id, null, CacheDuration);
                return null;
            }

            var screenshotPath = ResolveAssetPath(reportDirectory, assets.ScreenshotPath);
            var videoPath = ResolveAssetPath(reportDirectory, assets.VideoPath);

            var assetInfo = new ReportAssetInfo(
                screenshotPath != null ? _urlService.GetReportUrl(screenshotPath) : null,
                videoPath != null ? _urlService.GetReportUrl(videoPath) : null);

            _cache.Set<ReportAssetInfo?>(testResult.Id, assetInfo, CacheDuration);
            return assetInfo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse report.json for {ReportDirectory}", reportDirectory);
            _cache.Set<ReportAssetInfo?>(testResult.Id, null, CacheDuration);
            return null;
        }
    }

    private string? ResolveReportDirectory(string? reportDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(reportDirectoryPath))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_baseDirectory))
        {
            return null;
        }

        var trimmed = reportDirectoryPath.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            var fullPath = Path.GetFullPath(trimmed);
            return IsUnderBaseDirectory(fullPath) ? fullPath : null;
        }

        var combined = Path.GetFullPath(Path.Combine(_baseDirectory, trimmed));
        return IsUnderBaseDirectory(combined) ? combined : null;
    }

    private string? ResolveAssetPath(string reportDirectory, string? assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        var normalized = assetPath.Replace('/', Path.DirectorySeparatorChar);
        var combined = Path.GetFullPath(Path.Combine(reportDirectory, normalized));
        var reportDir = Path.GetFullPath(reportDirectory);
        var reportDirWithSeparator = reportDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return combined.StartsWith(reportDirWithSeparator, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(combined, reportDir, StringComparison.OrdinalIgnoreCase)
            ? combined
            : null;
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

    private static AssetMatch? FindAssets(JsonElement root, string testFullName)
    {
        if (!root.TryGetProperty("results", out var resultsElement) || resultsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var result in resultsElement.EnumerateArray())
        {
            var match = FindInSuite(result, testFullName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static AssetMatch? FindInSuite(JsonElement suite, string testFullName)
    {
        if (suite.TryGetProperty("tests", out var testsElement) && testsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var test in testsElement.EnumerateArray())
            {
                if (TryMatchTest(test, testFullName, out var match))
                {
                    return match;
                }
            }
        }

        if (suite.TryGetProperty("suites", out var suitesElement) && suitesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var nested in suitesElement.EnumerateArray())
            {
                var match = FindInSuite(nested, testFullName);
                if (match != null)
                {
                    return match;
                }
            }
        }

        return null;
    }

    private static bool TryMatchTest(JsonElement test, string testFullName, out AssetMatch match)
    {
        match = null!;

        var fullTitle = test.TryGetProperty("fullTitle", out var fullTitleElement)
            ? fullTitleElement.GetString()
            : null;

        var title = test.TryGetProperty("title", out var titleElement)
            ? titleElement.GetString()
            : null;

        var candidates = BuildTitleCandidates(testFullName);
        if (!candidates.Any(candidate => IsTitleMatch(candidate, fullTitle) || IsTitleMatch(candidate, title)))
        {
            return false;
        }

        if (!test.TryGetProperty("context", out var contextElement))
        {
            return false;
        }

        var assetPaths = ExtractAssetPaths(contextElement);
        if (assetPaths.Count == 0)
        {
            return false;
        }

        match = new AssetMatch(
            assetPaths.FirstOrDefault(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)),
            assetPaths.FirstOrDefault(path => path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)));

        return !string.IsNullOrEmpty(match.ScreenshotPath) || !string.IsNullOrEmpty(match.VideoPath);
    }

    private static List<string> ExtractAssetPaths(JsonElement contextElement)
    {
        if (contextElement.ValueKind == JsonValueKind.Null)
        {
            return new List<string>();
        }

        if (contextElement.ValueKind == JsonValueKind.Array)
        {
            return contextElement.EnumerateArray()
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)
                .ToList();
        }

        if (contextElement.ValueKind == JsonValueKind.String)
        {
            var raw = contextElement.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<string>();
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(raw);
                return parsed?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).ToList()
                    ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        return new List<string>();
    }

    private static List<string> BuildTitleCandidates(string testFullName)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(testFullName))
        {
            candidates.Add(testFullName);

            var dotIndex = testFullName.IndexOf('.', StringComparison.Ordinal);
            if (dotIndex > 0 && dotIndex < testFullName.Length - 1)
            {
                var left = testFullName[..dotIndex];
                var right = testFullName[(dotIndex + 1)..];
                candidates.Add(left);
                candidates.Add(right);
            }
        }

        return candidates
            .Select(NormalizeTitle)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsTitleMatch(string? candidate, string? source)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        var normalizedCandidate = NormalizeTitle(candidate);
        var normalizedSource = NormalizeTitle(source);

        if (string.IsNullOrEmpty(normalizedCandidate) || string.IsNullOrEmpty(normalizedSource))
        {
            return false;
        }

        if (string.Equals(normalizedCandidate, normalizedSource, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return normalizedCandidate.Contains(normalizedSource, StringComparison.OrdinalIgnoreCase) ||
               normalizedSource.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTitle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\n", " ").Replace("\r", " ");
        return System.Text.RegularExpressions.Regex.Replace(normalized, "\\s+", " ").Trim();
    }

    private sealed record AssetMatch(string? ScreenshotPath, string? VideoPath);
}
