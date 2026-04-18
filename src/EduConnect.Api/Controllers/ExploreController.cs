using System.Globalization;
using System.Text;
using EduConnect.Api.Common;
using EduConnect.Application.Contracts.Explore;
using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ExploreController(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IMemoryCache memoryCache) : ControllerBase
{
    [HttpGet("discovery")]
    public async Task<ActionResult<ExploreDiscoveryResponse>> GetDiscovery(
        [FromQuery] string? tab,
        [FromQuery] string? query,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var normalizedTab = NormalizeTab(tab);
        var normalizedQuery = NormalizeQuery(query);
        var currentUserUniversityId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId.Value)
            .Select(x => x.UniversityId)
            .FirstOrDefaultAsync(cancellationToken);

        var trends = await BuildLiveTrendResponsesAsync(
            normalizedTab,
            currentUserUniversityId,
            normalizedQuery,
            cancellationToken);

        var suggestions = SuggestionSeeds
            .Select(item => new ExploreSuggestionResponse
            {
                Id = item.Id,
                Name = item.Name,
                Handle = item.Handle,
                AvatarSeed = item.AvatarSeed,
                AvatarUrl = item.AvatarUrl,
                TargetPath = item.TargetPath,
                CtaLabel = item.CtaLabel,
                ReasonLabel = item.ReasonLabel,
                ActionableUserId = item.ActionableUserId,
                IsFollowedByCurrentUser = false
            })
            .ToArray();

        var liveSuggestions = await BuildLiveSuggestionsAsync(userId.Value, 4, cancellationToken);
        if (liveSuggestions.Length > 0)
        {
            suggestions = liveSuggestions;
        }

        return Ok(new ExploreDiscoveryResponse
        {
            Trends = trends,
            Suggestions = suggestions
        });
    }

    private async Task<IReadOnlyCollection<ExploreTrendItemResponse>> BuildLiveTrendResponsesAsync(
        string tab,
        Guid? currentUserUniversityId,
        string? query,
        CancellationToken cancellationToken)
    {
        const int maxResponseCount = 12;
        var liveTrendData = await GetCachedLiveTrendDataAsync(currentUserUniversityId, cancellationToken);

        if (liveTrendData.Aggregates.Count == 0)
        {
            return [];
        }

        var liveTrends = liveTrendData.Aggregates
            .Select(aggregate => BuildLiveTrendResponse(
                aggregate,
                tab,
                hasUniversityScope: liveTrendData.HasUniversityScope))
            .Where(item => ShouldIncludeInTab(item, tab))
            .ToArray();

        if (liveTrends.Length == 0)
        {
            return [];
        }

        var filteredLiveTrends = string.IsNullOrWhiteSpace(query)
            ? liveTrends
            : liveTrends.Where(item => MatchesQuery(item, query)).ToArray();

        return filteredLiveTrends
            .Take(maxResponseCount)
            .ToArray();
    }

    private async Task<CachedLiveTrendData> GetCachedLiveTrendDataAsync(
        Guid? currentUserUniversityId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"explore:live-trends:{currentUserUniversityId?.ToString() ?? "global"}";

        return await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await LoadLiveTrendDataAsync(currentUserUniversityId, cancellationToken);
        }) ?? CachedLiveTrendData.Empty;
    }

    private async Task<CachedLiveTrendData> LoadLiveTrendDataAsync(
        Guid? currentUserUniversityId,
        CancellationToken cancellationToken)
    {
        const int trendLimit = 24;
        var sinceUtc = DateTime.UtcNow.AddDays(-7);
        var recentPosts = await dbContext.Posts
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.CreatedAtUtc >= sinceUtc && x.Content.Contains("#"))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new PostTrendSource(
                x.Content,
                x.UserId,
                x.User.UniversityId,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var scopedPosts = currentUserUniversityId.HasValue
            ? recentPosts.Where(x => x.UniversityId == currentUserUniversityId.Value).ToArray()
            : Array.Empty<PostTrendSource>();
        var selectedPosts = scopedPosts.Length > 0
            ? scopedPosts.AsEnumerable()
            : recentPosts.AsEnumerable();
        var aggregates = PostTrendAnalyzer.Build(selectedPosts, trendLimit);

        return new CachedLiveTrendData(
            HasUniversityScope: scopedPosts.Length > 0,
            Aggregates: aggregates);
    }

    private async Task<ExploreSuggestionResponse[]> BuildLiveSuggestionsAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 8);

        var currentUserContext = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new CurrentUserSuggestionContext(
                x.UniversityId,
                x.StudentProfile != null ? x.StudentProfile.Department : null))
            .FirstOrDefaultAsync(cancellationToken);

        if (currentUserContext is null)
        {
            return [];
        }

        var followedUserIds = await dbContext.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowerUserId == userId)
            .Select(x => x.FollowedUserId)
            .ToArrayAsync(cancellationToken);

        var currentGroupIds = await dbContext.GroupMembers
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.GroupId)
            .ToArrayAsync(cancellationToken);

        var activeSinceUtc = DateTime.UtcNow.AddDays(-14);

        var suggestionCandidates = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.IsActive && x.Id != userId)
            .Select(x => new FollowSuggestionCandidate(
                x.Id,
                x.FullName,
                x.StudentProfile != null ? x.StudentProfile.AvatarUrl : null,
                x.StudentProfile != null ? x.StudentProfile.Department : null,
                x.UniversityId,
                x.University != null ? x.University.Name : null,
                followedUserIds.Contains(x.Id),
                x.GroupMemberships.Count(membership => currentGroupIds.Contains(membership.GroupId)),
                x.Posts.Count(post => !post.IsDeleted && !post.GroupId.HasValue && post.CreatedAtUtc >= activeSinceUtc)))
            .ToListAsync(cancellationToken);

        return suggestionCandidates
            .OrderBy(x => x.IsFollowedByCurrentUser)
            .ThenByDescending(x => x.UniversityId == currentUserContext.UniversityId && currentUserContext.UniversityId.HasValue)
            .ThenByDescending(x => x.MutualGroupCount)
            .ThenByDescending(x => x.RecentPersonalPostCount)
            .ThenBy(x => x.FullName)
            .Take(limit)
            .Select(x => new ExploreSuggestionResponse
            {
                Id = x.Id.ToString(),
                Name = x.FullName,
                Handle = $"@{BuildHandle(x.FullName)}",
                AvatarSeed = x.Id.ToString("N"),
                AvatarUrl = x.AvatarUrl,
                TargetPath = $"/profile/{x.Id}",
                CtaLabel = "Takip et",
                ReasonLabel = BuildFollowSuggestionReason(x, currentUserContext),
                ActionableUserId = x.Id.ToString(),
                IsFollowedByCurrentUser = x.IsFollowedByCurrentUser
            })
            .ToArray();
    }

    private static bool MatchesQuery(ExploreTrendSeed item, string query)
    {
        return MatchesSearch(query, item.ContextLabel, item.Title, item.MetricLabel);
    }

    private static bool MatchesQuery(ExploreTrendItemResponse item, string query)
    {
        return MatchesSearch(query, item.ContextLabel, item.Title, item.MetricLabel);
    }

    private static string NormalizeTab(string? tab)
    {
        return tab?.Trim().ToLowerInvariant() switch
        {
            "campus" => "campus",
            _ => "for-you"
        };
    }

    private static ExploreTrendItemResponse BuildLiveTrendResponse(
        PostTrendAggregate aggregate,
        string requestedTab,
        bool hasUniversityScope)
    {
        var metadata = ClassifyTrend(aggregate.DisplayHashtag);

        return new ExploreTrendItemResponse
        {
            Id = $"trend-{aggregate.DisplayHashtag.TrimStart('#').ToLowerInvariant()}",
            PrimaryTab = metadata.PrimaryTab,
            Kind = metadata.Kind,
            ContextLabel = BuildContextLabel(metadata.PrimaryTab, requestedTab, hasUniversityScope),
            Title = aggregate.DisplayHashtag,
            MetricLabel = $"{aggregate.PostCount} gonderi",
            TargetPath = BuildTrendTargetPath(aggregate.DisplayHashtag, metadata.TargetPath)
        };
    }

    private static bool ShouldIncludeInTab(ExploreTrendItemResponse item, string tab)
    {
        return tab switch
        {
            "for-you" => true,
            "campus" => true,
            _ => string.Equals(item.PrimaryTab, tab, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static string BuildContextLabel(
        string primaryTab,
        string requestedTab,
        bool hasUniversityScope)
    {
        if (requestedTab == "for-you")
        {
            return hasUniversityScope ? "Kampus - Sana ozel" : "Platform - Sana ozel";
        }

        return hasUniversityScope
            ? "Kampus - Universitende gundemde"
            : "Platformda - Gundemdekiler";
    }

    private static LiveTrendMetadata ClassifyTrend(string title)
    {
        var normalizedTitle = title.Trim().TrimStart('#').ToLowerInvariant();

        if (ContainsAny(normalizedTitle, DiscountKeywords))
        {
            return new LiveTrendMetadata("campus", "discount", "/market?tab=discounts");
        }

        if (ContainsAny(normalizedTitle, CommunityKeywords))
        {
            return new LiveTrendMetadata("campus", "community", "/communities");
        }

        return new LiveTrendMetadata("campus", "hashtag", "/");
    }

    private static bool ContainsAny(string value, IEnumerable<string> keywords)
    {
        return keywords.Any(value.Contains);
    }

    private static string BuildTrendTargetPath(string title, string fallbackPath)
    {
        if (!title.TrimStart().StartsWith('#'))
        {
            return fallbackPath;
        }

        var tagValue = title.Trim().TrimStart('#');
        if (tagValue.Length == 0)
        {
            return fallbackPath;
        }

        return $"/explore/tag/{Uri.EscapeDataString(tagValue)}";
    }

    private static string? NormalizeQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var trimmedQuery = query.Trim();
        return trimmedQuery.Length <= 80
            ? trimmedQuery
            : trimmedQuery[..80];
    }

    private static bool MatchesSearch(string query, params string[] values)
    {
        var tokens = TokenizeSearch(query);
        if (tokens.Length == 0)
        {
            return true;
        }

        var searchableValue = SimplifySearchText(string.Join(' ', values));
        return tokens.All(searchableValue.Contains);
    }

    private static string[] TokenizeSearch(string query)
    {
        return SimplifySearchText(query)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string SimplifySearchText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedValue = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalizedValue.Length);

        foreach (var character in normalizedValue)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            if (char.IsWhiteSpace(character) || character is '-' or '_' or '#')
            {
                builder.Append(' ');
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static IReadOnlyCollection<ExploreTrendItemResponse> MergeTrendResponses(
        IEnumerable<ExploreTrendItemResponse> primary,
        IEnumerable<ExploreTrendItemResponse> fallback,
        int limit)
    {
        var merged = new List<ExploreTrendItemResponse>(limit);
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in primary.Concat(fallback))
        {
            var dedupeKey = $"{item.Title}|{item.TargetPath}";
            if (!seenKeys.Add(dedupeKey))
            {
                continue;
            }

            merged.Add(item);
            if (merged.Count >= limit)
            {
                break;
            }
        }

        return merged;
    }

    private static IReadOnlyCollection<ExploreTrendItemResponse> BuildStaticTrendResponses(
        string tab,
        string? query)
    {
        var trends = TrendSeeds
            .Where(item => tab == "for-you"
                ? item.ShowInForYou
                : string.Equals(item.PrimaryTab, tab, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(query) || MatchesQuery(item, query))
            .Select(item => new ExploreTrendItemResponse
            {
                Id = item.Id,
                PrimaryTab = item.PrimaryTab,
                Kind = item.Kind,
                ContextLabel = item.ContextLabel,
                Title = item.Title,
                MetricLabel = item.MetricLabel,
                TargetPath = BuildTrendTargetPath(item.Title, item.TargetPath)
            })
            .ToArray();

        return trends;
    }

    private static readonly ExploreTrendSeed[] TrendSeeds =
    [
        new(
            "campus-1",
            "campus",
            "hashtag",
            "Kampus - Universitende gundemde",
            "#kutuphanegeceacik",
            "38 gonderi",
            "/",
            true),
        new(
            "campus-2",
            "campus",
            "hashtag",
            "Kampus - Gunun konusu",
            "#yemekhanegundemi",
            "14 yeni paylasim",
            "/",
            true),
        new(
            "campus-3",
            "campus",
            "community",
            "Topluluklar - Kampuste yukseliyor",
            "#acikkaynakatolyesi",
            "11 gonderi",
            "/communities",
            false),
        new(
            "campus-4",
            "campus",
            "hashtag",
            "Kampus - Universitende gundemde",
            "#finalhaftasi",
            "52 gonderi",
            "/",
            true),
        new(
            "campus-5",
            "campus",
            "hashtag",
            "Kampus - Gundemdekiler",
            "#bitirmeprojesunumlari",
            "9 guncel paylasim",
            "/",
            true),
        new(
            "campus-6",
            "campus",
            "hashtag",
            "Kampus - Son 24 saatte hizlandi",
            "#labtelafisi",
            "17 gonderi",
            "/",
            false),
        new(
            "campus-7",
            "campus",
            "hashtag",
            "Kampus - Gundemdekiler",
            "#yazstaji2026",
            "29 gonderi",
            "/",
            true),
        new(
            "campus-8",
            "campus",
            "hashtag",
            "Kampus - Bu hafta one cikiyor",
            "#kariyergunleri",
            "13 yeni paylasim",
            "/",
            true),
        new(
            "campus-9",
            "campus",
            "discount",
            "Kampus - Ogrenci firsati",
            "#cvbaskiindirimi",
            "6 paylasim",
            "/market?tab=discounts",
            false),
        new(
            "campus-10",
            "campus",
            "event",
            "Kampus - Bu hafta yukseliyor",
            "#ieeeworkshop",
            "21 gonderi",
            "/events",
            true),
        new(
            "campus-11",
            "campus",
            "event",
            "Kampus - Kayitlar acildi",
            "#acikhavafilmgecesi",
            "84 katilim goruntulendi",
            "/events",
            true),
        new(
            "campus-12",
            "campus",
            "event",
            "Kampus - Gundemdekiler",
            "#kariyerzirvesi",
            "18 gonderi",
            "/events",
            false)
    ];

    private static readonly ExploreSuggestionSeed[] SuggestionSeeds =
    [
        new(
            "suggestion-1",
            "IEEE Ogrenci Kulubu",
            "@ieeerteu",
            "IEEEKulubu",
            null,
            "/communities",
            "Incele",
            "Toplulugu kesfet",
            null),
        new(
            "suggestion-2",
            "Kariyer Merkezi",
            "@kariyermerkezi",
            "KariyerMerkezi",
            null,
            "/events",
            "Incele",
            "Etkinlikleri gor",
            null),
        new(
            "suggestion-3",
            "Kampus Duyurular",
            "@kampusduyurular",
            "KampusDuyurular",
            null,
            "/",
            "Incele",
            "Gundeme don",
            null)
    ];

    private sealed record ExploreTrendSeed(
        string Id,
        string PrimaryTab,
        string Kind,
        string ContextLabel,
        string Title,
        string MetricLabel,
        string TargetPath,
        bool ShowInForYou);

    private sealed record ExploreSuggestionSeed(
        string Id,
        string Name,
        string Handle,
        string AvatarSeed,
        string? AvatarUrl,
        string TargetPath,
        string CtaLabel,
        string? ReasonLabel,
        string? ActionableUserId);

    private sealed record CurrentUserSuggestionContext(
        Guid? UniversityId,
        string? Department);

    private sealed record FollowSuggestionCandidate(
        Guid Id,
        string FullName,
        string? AvatarUrl,
        string? Department,
        Guid? UniversityId,
        string? UniversityName,
        bool IsFollowedByCurrentUser,
        int MutualGroupCount,
        int RecentPersonalPostCount);

    private sealed record LiveTrendMetadata(
        string PrimaryTab,
        string Kind,
        string TargetPath);

    private sealed record CachedLiveTrendData(
        bool HasUniversityScope,
        IReadOnlyCollection<PostTrendAggregate> Aggregates)
    {
        public static CachedLiveTrendData Empty { get; } = new(false, []);
    }

    private static readonly string[] DiscountKeywords =
    [
        "indirim",
        "firsat",
        "kampanya",
        "kupon",
        "baski",
        "discount"
    ];

    private static readonly string[] CommunityKeywords =
    [
        "topluluk",
        "kulup",
        "community",
        "ieee"
    ];

    private static string BuildFollowSuggestionReason(
        FollowSuggestionCandidate suggestion,
        CurrentUserSuggestionContext currentUser)
    {
        if (suggestion.MutualGroupCount > 0)
        {
            return $"{suggestion.MutualGroupCount} ortak topluluk";
        }

        if (suggestion.UniversityId == currentUser.UniversityId && currentUser.UniversityId.HasValue)
        {
            return "Ayni universiteden";
        }

        if (!string.IsNullOrWhiteSpace(suggestion.Department) &&
            string.Equals(suggestion.Department, currentUser.Department, StringComparison.OrdinalIgnoreCase))
        {
            return "Benzer bolum ilgisi";
        }

        if (suggestion.RecentPersonalPostCount > 0)
        {
            return "Son gunlerde aktif";
        }

        return "Kesfet icin oneriliyor";
    }

    private static string BuildHandle(string fullName)
    {
        var simplified = SimplifySearchText(fullName).Replace(" ", string.Empty);
        return string.IsNullOrWhiteSpace(simplified) ? "educonnect" : simplified;
    }
}
