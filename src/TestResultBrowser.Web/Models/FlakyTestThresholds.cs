namespace TestResultBrowser.Web.Models;

/// <summary>
/// Flaky test detection threshold configuration
/// </summary>
public class FlakyTestThresholds
{
    /// <summary>
    /// Number of recent runs to analyze for flaky detection
    /// Default: 20
    /// </summary>
    public int RollingWindowSize { get; set; } = 20;

    /// <summary>
    /// Percentage of failures in rolling window to trigger flaky status (0-100)
    /// Default: 30
    /// </summary>
    public int FlakinessTriggerPercentage { get; set; } = 30;

    /// <summary>
    /// Alias for FlakinessTriggerPercentage (for consistency with UI settings)
    /// </summary>
    public int TriggerPercentage
    {
        get => FlakinessTriggerPercentage;
        set => FlakinessTriggerPercentage = value;
    }

    /// <summary>
    /// Number of consecutive passes required to clear flaky status
    /// Default: 10
    /// </summary>
    public int ClearAfterConsecutivePasses { get; set; } = 10;
}
