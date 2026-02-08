using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Extracts report assets (screenshots/videos) from report.json on-demand.
/// </summary>
public class ReportAssetService : IReportAssetService
{
    private readonly ConcurrentDictionary<string, ReportAssetInfo?> _cache = new();
    private readonly ITestReportUrlService _urlService;
    private readonly string _baseDirectory;
    private readonly ILogger<ReportAssetService> _logger;

    public ReportAssetService(
        ITestReportUrlService urlService,
        IOptions<TestResultBrowserOptions> options,
        ILogger<ReportAssetService> logger)
    {
        _urlService = urlService;
        _logger = logger;
        _baseDirectory = Path.GetFullPath(options?.Value.FileSharePath ?? string.Empty);
    }

    public Task<ReportAssetInfo?> GetAssetsAsync(TestResult testResult)
    {
        if (testResult == null)
        {
            return Task.FromResult<ReportAssetInfo?>(null);
        }

        if (_cache.TryGetValue(testResult.Id, out var cached))
        {
            return Task.FromResult<ReportAssetInfo?>(cached);
        }

        var cachedFromResult = TryBuildFromCachedPaths(testResult);
        if (cachedFromResult != null)
        {
            _cache[testResult.Id] = cachedFromResult;
            return Task.FromResult<ReportAssetInfo?>(cachedFromResult);
        }

        var reportDirectory = ResolveReportDirectory(testResult.ReportDirectoryPath);
        if (string.IsNullOrEmpty(reportDirectory))
        {
            _cache[testResult.Id] = null;
            return Task.FromResult<ReportAssetInfo?>(null);
        }

        var reportJsonPath = Path.Combine(reportDirectory, "report.json");
        if (!File.Exists(reportJsonPath))
        {
            _cache[testResult.Id] = null;
            return Task.FromResult<ReportAssetInfo?>(null);
        }

        try
        {
            using var stream = File.OpenRead(reportJsonPath);
            using var doc = JsonDocument.Parse(stream);

            var assets = FindAssets(doc.RootElement, testResult.TestFullName);
            if (assets == null)
            {
                _cache[testResult.Id] = null;
                return Task.FromResult<ReportAssetInfo?>(null);
            }

            var screenshotPath = ResolveAssetPath(reportDirectory, assets.ScreenshotPath);
            var videoPath = ResolveAssetPath(reportDirectory, assets.VideoPath);

            testResult.ReportScreenshotPath = screenshotPath;
            testResult.ReportVideoPath = videoPath;

            var assetInfo = new ReportAssetInfo(
                screenshotPath != null ? _urlService.GetReportUrl(screenshotPath) : null,
                videoPath != null ? _urlService.GetReportUrl(videoPath) : null);

            _cache[testResult.Id] = assetInfo;
            return Task.FromResult<ReportAssetInfo?>(assetInfo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse report.json for {ReportDirectory}", reportDirectory);
            _cache[testResult.Id] = null;
            return Task.FromResult<ReportAssetInfo?>(null);
        }
    }

    private ReportAssetInfo? TryBuildFromCachedPaths(TestResult testResult)
    {
        if (string.IsNullOrEmpty(testResult.ReportScreenshotPath) &&
            string.IsNullOrEmpty(testResult.ReportVideoPath))
        {
            return null;
        }

        return new ReportAssetInfo(
            string.IsNullOrEmpty(testResult.ReportScreenshotPath) ? null : _urlService.GetReportUrl(testResult.ReportScreenshotPath),
            string.IsNullOrEmpty(testResult.ReportVideoPath) ? null : _urlService.GetReportUrl(testResult.ReportVideoPath));
    }

    private string? ResolveReportDirectory(string? reportDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(reportDirectoryPath))
        {
            return null;
        }

        var trimmed = reportDirectoryPath.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            var fullPath = Path.GetFullPath(trimmed);
            return IsUnderBaseDirectory(fullPath) ? fullPath : null;
        }

        if (string.IsNullOrWhiteSpace(_baseDirectory))
        {
            return null;
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
        return combined.StartsWith(Path.GetFullPath(reportDirectory), StringComparison.OrdinalIgnoreCase)
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
