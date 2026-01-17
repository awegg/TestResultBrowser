namespace TestResultBrowser.Web.Models;

/// <summary>
/// Configuration matrix across Version × NamedConfig for a given build
/// </summary>
public record ConfigurationMatrix
{
    /// <summary>Distinct versions present in the build results (e.g., dev, PXrel114)</summary>
    public required List<string> Versions { get; init; }

    /// <summary>Distinct named configs present (e.g., Default1, Win2022)</summary>
    public required List<string> NamedConfigs { get; init; }

    /// <summary>Cells keyed by (Version, NamedConfig)</summary>
    public required Dictionary<string, Dictionary<string, ConfigCell>> Cells { get; init; }
}
