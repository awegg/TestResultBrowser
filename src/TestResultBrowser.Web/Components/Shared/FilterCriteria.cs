namespace TestResultBrowser.Web.Components.Shared;

/// <summary>
/// Filter criteria for test results
/// </summary>
public class FilterCriteria
{
    public List<string> Domains { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public List<string> Versions { get; set; } = new();
    public List<string> Configurations { get; set; } = new();
    public List<string> Builds { get; set; } = new();
    public List<string> Statuses { get; set; } = new();
}
