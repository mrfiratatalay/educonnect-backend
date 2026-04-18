using System.Text.Json.Serialization;

namespace EduConnect.Application.Interfaces;

public interface INlpService
{
    Task<NlpClassifyResult> ClassifyAsync(string text, CancellationToken cancellationToken = default);
}

public sealed class NlpClassifyResult
{
    public string Intent { get; init; } = string.Empty;
    public double Confidence { get; init; }
    [JsonPropertyName("confidence_band")]
    public string ConfidenceBand { get; init; } = "low";
    [JsonPropertyName("needs_review")]
    public bool NeedsReview { get; init; }
    [JsonPropertyName("resolver_used")]
    public bool ResolverUsed { get; init; }
    public IReadOnlyCollection<NlpEntity> Entities { get; init; } = [];

    [JsonPropertyName("model_used")]
    public string ModelUsed { get; init; } = string.Empty;

    [JsonPropertyName("kb_answer")]
    public NlpKbMatch? KbAnswer { get; init; }
}

public sealed class NlpKbMatch
{
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public double Score { get; init; }

    [JsonPropertyName("source_url")]
    public string SourceUrl { get; init; } = string.Empty;

    [JsonPropertyName("source_title")]
    public string SourceTitle { get; init; } = string.Empty;

    public string Confidence { get; init; } = "medium";

    [JsonPropertyName("time_sensitive")]
    public bool TimeSensitive { get; init; }

    [JsonPropertyName("faculty_scope")]
    public string FacultyScope { get; init; } = "general";

    public string Topic { get; init; } = string.Empty;
}

public sealed class NlpEntity
{
    public string Value { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Start { get; init; }
    public int End { get; init; }
}
