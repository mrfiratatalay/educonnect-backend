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
                TargetPath = item.TargetPath,
                CtaLabel = item.CtaLabel
            })
            .ToArray();

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
            return BuildStaticTrendResponses(tab, query);
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
            return BuildStaticTrendResponses(tab, query);
        }

        var filteredLiveTrends = string.IsNullOrWhiteSpace(query)
            ? liveTrends
            : liveTrends.Where(item => MatchesQuery(item, query)).ToArray();

        var staticFallbackTrends = BuildStaticTrendResponses(tab, query);
        return MergeTrendResponses(filteredLiveTrends, staticFallbackTrends, maxResponseCount);
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
            "academic" => "academic",
            "career" => "career",
            "events" => "events",
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
            return primaryTab switch
            {
                "academic" => "Akademik - Sana ozel",
                "career" => "Kariyer - Sana ozel",
                "events" => "Etkinlikler - Sana ozel",
                _ => hasUniversityScope ? "Kampus - Sana ozel" : "Platform - Sana ozel"
            };
        }

        return primaryTab switch
        {
            "academic" => hasUniversityScope
                ? "Akademik - Universitende gundemde"
                : "Akademik - Platformda gundemde",
            "career" => hasUniversityScope
                ? "Kariyer - Kampuste konusuluyor"
                : "Kariyer - Platformda konusuluyor",
            "events" => hasUniversityScope
                ? "Etkinlikler - Bu hafta yukseliyor"
                : "Etkinlikler - Platformda yukseliyor",
            _ => hasUniversityScope
                ? "Kampus - Universitende gundemde"
                : "Platformda - Gundemdekiler"
        };
    }

    private static LiveTrendMetadata ClassifyTrend(string title)
    {
        var normalizedTitle = title.Trim().TrimStart('#').ToLowerInvariant();

        if (ContainsAny(normalizedTitle, EventKeywords))
        {
            return new LiveTrendMetadata("events", "event", "/events");
        }

        if (ContainsAny(normalizedTitle, CareerKeywords))
        {
            return new LiveTrendMetadata("career", "hashtag", "/");
        }

        if (ContainsAny(normalizedTitle, AcademicKeywords))
        {
            return new LiveTrendMetadata("academic", "hashtag", "/");
        }

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
            "academic-1",
            "academic",
            "hashtag",
            "Akademik - Universitende gundemde",
            "#finalhaftasi",
            "52 gonderi",
            "/",
            true),
        new(
            "academic-2",
            "academic",
            "hashtag",
            "Akademik - Duyurularda one cikiyor",
            "#bitirmeprojesunumlari",
            "9 guncel paylasim",
            "/",
            true),
        new(
            "academic-3",
            "academic",
            "hashtag",
            "Akademik - Son 24 saatte hizlandi",
            "#labtelafisi",
            "17 gonderi",
            "/",
            false),
        new(
            "career-1",
            "career",
            "hashtag",
            "Kariyer - Kampuste konusuluyor",
            "#yazstaji2026",
            "29 gonderi",
            "/",
            true),
        new(
            "career-2",
            "career",
            "hashtag",
            "Kariyer - Bu hafta one cikiyor",
            "#kariyergunleri",
            "13 yeni paylasim",
            "/events",
            true),
        new(
            "career-3",
            "career",
            "discount",
            "Kariyer - Ogrenci firsati",
            "#cvbaskiindirimi",
            "6 paylasim",
            "/market?tab=discounts",
            false),
        new(
            "events-1",
            "events",
            "event",
            "Etkinlikler - Bu hafta yukseliyor",
            "#ieeeworkshop",
            "21 gonderi",
            "/events",
            true),
        new(
            "events-2",
            "events",
            "event",
            "Etkinlikler - Kayitlar acildi",
            "#acikhavafilmgecesi",
            "84 katilim goruntulendi",
            "/events",
            true),
        new(
            "events-3",
            "events",
            "event",
            "Etkinlikler - Kampuste konusuluyor",
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
            "/communities",
            "Takip et"),
        new(
            "suggestion-2",
            "Kariyer Merkezi",
            "@kariyermerkezi",
            "KariyerMerkezi",
            "/events",
            "Takip et"),
        new(
            "suggestion-3",
            "Kampus Duyurular",
            "@kampusduyurular",
            "KampusDuyurular",
            "/",
            "Takip et")
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
        string TargetPath,
        string CtaLabel);

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

    private static readonly string[] AcademicKeywords =
    [
        "final",
        "vize",
        "sinav",
        "quiz",
        "but",
        "tez",
        "proje",
        "sunum",
        "lab",
        "odev",
        "ders",
        "studyjam"
    ];

    private static readonly string[] CareerKeywords =
    [
        "kariyer",
        "staj",
        "cv",
        "mulakat",
        "intern",
        "linkedin",
        "network",
        "portfolyo",
        "portfolio",
        "mentor"
    ];

    private static readonly string[] EventKeywords =
    [
        "workshop",
        "etkinlik",
        "seminer",
        "hackathon",
        "zirve",
        "summit",
        "meetup",
        "bulusma",
        "film",
        "konser",
        "atolye",
        "festival"
    ];

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
}
