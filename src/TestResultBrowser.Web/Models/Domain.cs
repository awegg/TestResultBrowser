namespace TestResultBrowser.Web.Models;

/// <summary>
/// Top-level organizational unit (Core, T&T, PM, Prod, Feature)
/// </summary>
public record Domain
{
    /// <summary>Domain ID extracted from file path, e.g., "Core", "TnT_Prod"</summary>
    public required string Id { get; init; }
    
    /// <summary>Human-readable display name, e.g., "Px Core", "Px T&T Production"</summary>
    public required string DisplayName { get; init; }
    
    /// <summary>List of feature IDs belonging to this domain</summary>
    public List<string> FeatureIds { get; init; } = new();
}
