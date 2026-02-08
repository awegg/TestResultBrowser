namespace TestResultBrowser.Web.Models;

/// <summary>
/// Diagnostic statistics for string pooling.
/// </summary>
public record StringPoolStats(int UniqueCount, int InternedCount, int HitCount, int MissCount);
