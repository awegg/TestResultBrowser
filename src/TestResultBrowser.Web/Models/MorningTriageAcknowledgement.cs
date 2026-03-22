namespace TestResultBrowser.Web.Models;

public class MorningTriageAcknowledgement
{
    public string Id { get; set; } = string.Empty;

    public string TestFullName { get; set; } = string.Empty;

    public string ConfigurationId { get; set; } = string.Empty;

    public string FailureSignature { get; set; } = string.Empty;

    public string DomainId { get; set; } = string.Empty;

    public string FeatureId { get; set; } = string.Empty;

    public string AcknowledgedBy { get; set; } = string.Empty;

    public DateTime AcknowledgedAt { get; set; }

    public string? Note { get; set; }
}
