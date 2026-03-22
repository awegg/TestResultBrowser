using System.Text.RegularExpressions;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

public class FailureClassificationService : IFailureClassificationService
{
    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

    private static readonly string[] InfrastructureMarkers =
    {
        "connection refused",
        "timed out",
        "timeout",
        "network",
        "socket",
        "503 service unavailable",
        "service unavailable",
        "could not connect",
        "connection reset",
        "dns",
        "authentication failed",
        "access denied",
        "login failed",
        "http 5",
        "transport error"
    };

    private static readonly string[] SetupMarkers =
    {
        "beforeall",
        "beforeeach",
        "setup",
        "teardown",
        "fixture setup",
        "testinitialize",
        "class initialize"
    };

    public MorningFailureCategory Classify(TestResult testResult)
    {
        if (testResult.IsLifecycleHook)
        {
            return MorningFailureCategory.Setup;
        }

        var combined = $"{testResult.ErrorMessage} {testResult.StackTrace}".ToLowerInvariant();
        if (SetupMarkers.Any(marker => combined.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return MorningFailureCategory.Setup;
        }

        if (InfrastructureMarkers.Any(marker => combined.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return MorningFailureCategory.Infrastructure;
        }

        return MorningFailureCategory.Product;
    }

    public string BuildFailureSignature(TestResult testResult)
    {
        var text = string.IsNullOrWhiteSpace(testResult.ErrorMessage)
            ? testResult.StackTrace ?? string.Empty
            : testResult.ErrorMessage!;

        text = text.ToLowerInvariant();
        text = WhitespaceRegex.Replace(text, " ").Trim();

        if (text.Length > 180)
        {
            text = text[..180];
        }

        return string.IsNullOrWhiteSpace(text) ? "unknown-failure" : text;
    }
}
