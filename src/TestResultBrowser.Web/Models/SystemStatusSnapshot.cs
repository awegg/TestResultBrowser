namespace TestResultBrowser.Web.Models;

public sealed class SystemStatusSnapshot
{
    public int TotalResults { get; init; }

    public long MemoryUsageBytes { get; init; }

    public long ManagedMemoryBytes { get; init; }

    public long ProcessMemoryBytes { get; init; }

    public int TotalBuilds { get; init; }

    public int TotalConfigurations { get; init; }

    public int TotalDomains { get; init; }

    public DateTime? EarliestDate { get; init; }

    public DateTime? LatestDate { get; init; }

    public IReadOnlyList<string> RecentBuilds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Versions { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> NamedConfigs { get; init; } = Array.Empty<string>();
}