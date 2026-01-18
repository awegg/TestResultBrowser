namespace TestResultBrowser.Web.Models;

/// <summary>
/// Represents a single build column in the history grid
/// </summary>
public class HistoryColumn
{
    /// <summary>
    /// Build ID (e.g., "Release-252")
    /// </summary>
    public string BuildId { get; set; } = string.Empty;

    /// <summary>
    /// Build timestamp
    /// </summary>
    public DateTime BuildTime { get; set; }

    /// <summary>
    /// Column index for rendering (0 = latest, increases backwards in time)
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Display date string (e.g., "01/15/2026 22:55")
    /// </summary>
    public string DisplayDate => BuildTime.ToString("MM/dd/yyyy HH:mm");
}
