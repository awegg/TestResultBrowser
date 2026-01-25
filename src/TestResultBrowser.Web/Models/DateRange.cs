namespace TestResultBrowser.Web.Models;

/// <summary>
/// Represents a date range for filtering test results
/// </summary>
public record DateRange(DateTime StartDate, DateTime EndDate)
{
    /// <summary>
    /// Validates that StartDate is before or equal to EndDate
    /// </summary>
    public bool IsValid => StartDate <= EndDate;
}
