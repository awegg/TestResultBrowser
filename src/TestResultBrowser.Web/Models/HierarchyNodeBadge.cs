namespace TestResultBrowser.Web.Models;

public class HierarchyNodeBadge
{
    public required string Text { get; init; }

    public HierarchyBadgeTone Tone { get; init; } = HierarchyBadgeTone.Default;
}

public enum HierarchyBadgeTone
{
    Default,
    Info,
    Success,
    Warning,
    Error
}
