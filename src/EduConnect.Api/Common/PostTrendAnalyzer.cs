using System.Text.RegularExpressions;

namespace EduConnect.Api.Common;

internal static class PostTrendAnalyzer
{
    private static readonly Regex HashtagRegex = new(
        @"(?<![\p{L}\p{N}_])#([\p{L}\p{N}_]{2,40})",
        RegexOptions.Compiled);

    public static IReadOnlyCollection<PostTrendAggregate> Build(
        IEnumerable<PostTrendSource> posts,
        int limit)
    {
        var aggregates = new Dictionary<string, PostTrendAggregate>(StringComparer.OrdinalIgnoreCase);

        foreach (var post in posts)
        {
            foreach (var hashtag in ExtractHashtags(post.Content).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var normalizedHashtag = hashtag.ToLowerInvariant();

                if (!aggregates.TryGetValue(normalizedHashtag, out var aggregate))
                {
                    aggregate = new PostTrendAggregate(hashtag, post.CreatedAtUtc);
                    aggregates.Add(normalizedHashtag, aggregate);
                }

                aggregate.RegisterUsage(post.UserId, post.CreatedAtUtc);
            }
        }

        return aggregates.Values
            .OrderByDescending(x => x.PostCount)
            .ThenByDescending(x => x.UniqueAuthorCount)
            .ThenByDescending(x => x.LastUsedAtUtc)
            .Take(limit)
            .ToArray();
    }

    public static IReadOnlyCollection<string> ExtractHashtags(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var hashtags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in HashtagRegex.Matches(content))
        {
            var hashtagValue = match.Groups[1].Value.Trim();

            if (hashtagValue.Length == 0)
            {
                continue;
            }

            hashtags.Add($"#{hashtagValue}");
        }

        return hashtags.ToArray();
    }
}

internal sealed record PostTrendSource(
    string Content,
    Guid UserId,
    Guid? UniversityId,
    DateTime CreatedAtUtc);

internal sealed class PostTrendAggregate(string displayHashtag, DateTime lastUsedAtUtc)
{
    private readonly HashSet<Guid> userIds = [];

    public string DisplayHashtag { get; } = displayHashtag;

    public int PostCount { get; private set; }

    public int UniqueAuthorCount => userIds.Count;

    public DateTime LastUsedAtUtc { get; private set; } = lastUsedAtUtc;

    public void RegisterUsage(Guid userId, DateTime usedAtUtc)
    {
        PostCount += 1;
        userIds.Add(userId);

        if (usedAtUtc > LastUsedAtUtc)
        {
            LastUsedAtUtc = usedAtUtc;
        }
    }
}
