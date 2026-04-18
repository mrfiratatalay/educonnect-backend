namespace EduConnect.Application.Contracts.Explore;

public sealed class ExploreDiscoveryResponse
{
    public IReadOnlyCollection<ExploreTrendItemResponse> Trends { get; init; } = [];

    public IReadOnlyCollection<ExploreSuggestionResponse> Suggestions { get; init; } = [];
}

public sealed class ExploreTrendItemResponse
{
    public string Id { get; init; } = string.Empty;

    public string PrimaryTab { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string ContextLabel { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string MetricLabel { get; init; } = string.Empty;

    public string TargetPath { get; init; } = string.Empty;
}

public sealed class ExploreSuggestionResponse
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Handle { get; init; } = string.Empty;

    public string AvatarSeed { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public string TargetPath { get; init; } = string.Empty;

    public string CtaLabel { get; init; } = string.Empty;

    public string? ReasonLabel { get; init; }

    public string? ActionableUserId { get; init; }

    public bool IsFollowedByCurrentUser { get; init; }
}
