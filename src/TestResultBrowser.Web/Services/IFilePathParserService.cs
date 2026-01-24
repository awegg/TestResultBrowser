namespace TestResultBrowser.Web.Services;

/// <summary>
/// Result of parsing a test result file path
/// Contains extracted metadata from the file system path structure
/// </summary>
public record ParsedFilePath
{
    /// <summary>Build number extracted from Release-{BuildNumber}</summary>
    public required int BuildNumber { get; init; }

    /// <summary>Build ID, e.g., "Release-252"</summary>
    public required string BuildId { get; init; }

    /// <summary>Raw version string from path, e.g., "PXrel114", "dev"</summary>
    public required string VersionRaw { get; init; }

    /// <summary>Test type, e.g., "Regular", "Performance", "Smoke"</summary>
    public required string TestType { get; init; }

    /// <summary>Named configuration, e.g., "Win2019SQLServer2022"</summary>
    public required string NamedConfig { get; init; }

    /// <summary>Domain ID, e.g., "Core", "TnT_Prod"</summary>
    public required string DomainId { get; init; }

    /// <summary>Full file path</summary>
    public required string FilePath { get; init; }

    /// <summary>XML file name</summary>
    public required string FileName { get; init; }
}

/// <summary>
/// Service for parsing test result file paths to extract metadata
/// Expected pattern: {FileSharePath}\Release-{BuildNumber}\{Version}_{TestType}_{NamedConfig}_{Domain}\*.xml
/// </summary>
public interface IFilePathParserService
{
    /// <summary>
    /// Parses a file path to extract test metadata
    /// </summary>
    /// <param name="filePath">Full path to test result XML file</param>
    /// <returns>Parsed file path metadata with default values if path doesn't match expected pattern</returns>
    /// <example>
    /// ParseFilePath("\\\\server\\share\\Release-12345\\PXrel114_Regular_Win2019SQLServer2022_CORE\\TEST-UserService.xml")
    /// Returns: BuildNumber=12345, VersionRaw="PXrel114", TestType="Regular", NamedConfig="Win2019SQLServer2022", Domain="CORE"
    /// </example>
    ParsedFilePath ParseFilePath(string filePath);
}
