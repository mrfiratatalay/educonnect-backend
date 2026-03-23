using System.ComponentModel.DataAnnotations;

namespace EduConnect.Application.Contracts.Feedbacks;

public sealed class CreateFeedbackRequest
{
    [Required, StringLength(100)]
    public string FeatureArea { get; init; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; init; }

    [StringLength(1000)]
    public string? Comment { get; init; }
}
