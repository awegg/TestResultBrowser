namespace TestResultBrowser.Web.Models;

/// <summary>
/// Information about a selected test in a specific history column
/// </summary>
public class SelectedTestInfo
{
    /// <summary>The test node</summary>
    public required HierarchyNode TestNode { get; init; }

    /// <summary>The column index in the history (0 = latest build)</summary>
    public required int ColumnIndex { get; init; }
}
