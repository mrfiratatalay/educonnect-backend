namespace EduConnect.Infrastructure.Options;

public sealed class NlpServiceOptions
{
    public const string SectionName = "NlpService";

    public string BaseUrl { get; set; } = "http://localhost:8000";

    public int TimeoutSeconds { get; set; } = 10;

    public double ConfidenceThreshold { get; set; } = 0.7;
}
