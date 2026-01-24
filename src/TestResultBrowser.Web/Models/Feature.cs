namespace TestResultBrowser.Web.Models;

/// <summary>
/// Second-level organizational unit within a Domain (AlarmManager, UserManagement, etc.)
/// </summary>
public record Feature
{
    /// <summary>Feature ID extracted from test class name, e.g., "AlarmManager"</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name, e.g., "Alarm Manager"</summary>
    public required string DisplayName { get; init; }

    /// <summary>Parent domain ID</summary>
    public required string DomainId { get; init; }

    /// <summary>List of test suite IDs belonging to this feature</summary>
    public List<string> TestSuiteIds { get; init; } = new();
}
