namespace TestResultBrowser.Web.Models;

/// <summary>
/// Test configuration: combination of Version, TestType, NamedConfig, and Domain
/// Extracted from file path: {Version}_{TestType}_{NamedConfig}_{Domain}/
/// </summary>
public record Configuration
{
    /// <summary>Composite ID: {Version}_{TestType}_{NamedConfig}_{Domain}</summary>
    public required string Id { get; init; }

    /// <summary>Product version, e.g., "1.14.0" (mapped from "PXrel114")</summary>
    public required string Version { get; init; }

    /// <summary>Original version string from path, e.g., "PXrel114", "dev"</summary>
    public required string VersionRaw { get; init; }

    /// <summary>Test type, e.g., "Regular", "Performance", "Smoke"</summary>
    public required string TestType { get; init; }

    /// <summary>Named configuration, e.g., "Win2019SQLServer2022", "UbuntuMySQL"</summary>
    public required string NamedConfig { get; init; }

    /// <summary>Domain ID, e.g., "Core", "TnT_Prod"</summary>
    public required string DomainId { get; init; }

    /// <summary>Parsed OS from NamedConfig (if extractable)</summary>
    public string? OperatingSystem { get; init; }

    /// <summary>Parsed Database from NamedConfig (if extractable)</summary>
    public string? Database { get; init; }
}
