using EduConnect.Application.Contracts.Auth;
using EduConnect.Application.Contracts.Discounts;
using EduConnect.Application.Contracts.Events;
using EduConnect.Application.Contracts.Groups;
using EduConnect.Application.Contracts.Notifications;
using EduConnect.Application.Contracts.Posts;
using EduConnect.Application.Contracts.Products;
using EduConnect.Application.Contracts.Universities;
using EduConnect.Application.Contracts.Users;
using EduConnect.Application.Contracts.VisualSearch;
using EduConnect.Domain.Entities;
using EduConnect.Domain.Enums;
using System.Text.Json;

namespace EduConnect.Api.Mappings;

public static class MappingExtensions
{
    private const int GroupPreviewMemberLimit = 4;
    private const int GroupModeratorPreviewLimit = 3;

    public static AuthSessionResponse ToResponse(this AuthenticatedUserResult result)
    {
        return new AuthSessionResponse
        {
            AccessToken = result.AccessToken,
            ExpiresAtUtc = result.AccessTokenExpiresAtUtc,
            User = result.User.ToResponse()
        };
    }

    public static EmailVerificationChallengeResponse ToChallengeResponse(
        this EmailVerificationChallengeResult result,
        string message)
    {
        return new EmailVerificationChallengeResponse
        {
            Email = result.Email,
            VerificationExpiresAtUtc = result.VerificationExpiresAtUtc,
            CanResendAtUtc = result.CanResendAtUtc,
            Message = message
        };
    }

    public static UserProfileResponse ToResponse(this User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Department = user.StudentProfile?.Department ?? string.Empty,
            Year = user.StudentProfile?.Year ?? 1,
            Bio = user.StudentProfile?.Bio,
            AvatarUrl = user.StudentProfile?.AvatarUrl,
            CoverImageUrl = user.StudentProfile?.CoverImageUrl,
            UniversityId = user.UniversityId,
            UniversityName = user.University?.Name,
            FollowersCount = user.FollowerRelationships.Count,
            FollowingCount = user.FollowingRelationships.Count
        };
    }

    public static PublicUserProfileResponse ToPublicResponse(this User user, bool isFollowedByCurrentUser = false)
    {
        return new PublicUserProfileResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Role = user.Role,
            Department = user.StudentProfile?.Department ?? string.Empty,
            Year = user.StudentProfile?.Year ?? 1,
            Bio = user.StudentProfile?.Bio,
            AvatarUrl = user.StudentProfile?.AvatarUrl,
            CoverImageUrl = user.StudentProfile?.CoverImageUrl,
            UniversityId = user.UniversityId,
            UniversityName = user.University?.Name,
            FollowersCount = user.FollowerRelationships.Count,
            FollowingCount = user.FollowingRelationships.Count,
            IsFollowedByCurrentUser = isFollowedByCurrentUser
        };
    }

    public static UniversityOptionResponse ToResponse(this University university)
    {
        return new UniversityOptionResponse
        {
            Id = university.Id,
            Name = university.Name,
            City = university.City,
            Domain = university.Domain
        };
    }

    public static PostResponse ToResponse(this Post post, Guid? currentUserId, string? recommendationReason = null)
    {
        return new PostResponse
        {
            Id = post.Id,
            UserId = post.UserId,
            GroupId = post.GroupId,
            GroupName = post.Group?.Name,
            GroupSlug = post.Group?.Slug,
            GroupAvatarUrl = post.Group?.AvatarUrl,
            UserName = post.User.FullName,
            AvatarUrl = post.User.StudentProfile?.AvatarUrl,
            Content = post.Content,
            ImageUrl = post.ImageUrl,
            RecommendationReason = recommendationReason,
            LikesCount = post.Likes.Count,
            CommentsCount = post.Comments.Count,
            ViewsCount = post.Views.Count,
            LikedByCurrentUser = currentUserId.HasValue && post.Likes.Any(x => x.UserId == currentUserId.Value),
            BookmarkedByCurrentUser = currentUserId.HasValue && post.Bookmarks.Any(x => x.UserId == currentUserId.Value),
            CreatedAtUtc = post.CreatedAtUtc
        };
    }

    public static PostCommentResponse ToResponse(this PostComment comment)
    {
        return new PostCommentResponse
        {
            Id = comment.Id,
            UserId = comment.UserId,
            UserName = comment.User.FullName,
            AvatarUrl = comment.User.StudentProfile?.AvatarUrl,
            Content = comment.Content,
            CreatedAtUtc = comment.CreatedAtUtc
        };
    }

    public static GroupResponse ToResponse(this Group group, Guid? currentUserId)
    {
        return new GroupResponse
        {
            Id = group.Id,
            Name = group.Name,
            Slug = group.Slug,
            ShortDescription = group.ShortDescription,
            Description = group.Description,
            AvatarUrl = group.AvatarUrl,
            BannerUrl = group.BannerUrl,
            Category = group.Category,
            Rules = ParseGroupRules(group.RulesJson),
            CreatorUserId = group.CreatorUserId,
            CreatorName = group.CreatorUser.FullName,
            MemberCount = group.Members.Count,
            PreviewMembers = group.Members
                .OrderByDescending(x => x.JoinedAtUtc)
                .Take(GroupPreviewMemberLimit)
                .Select(x => x.ToPreviewResponse())
                .ToArray(),
            JoinedByCurrentUser = currentUserId.HasValue && group.Members.Any(x => x.UserId == currentUserId.Value),
            CreatedAtUtc = group.CreatedAtUtc
        };
    }

    public static GroupDetailResponse ToDetailResponse(
        this Group group,
        Guid? currentUserId,
        int postCount,
        int eventCount)
    {
        var summary = group.ToResponse(currentUserId);
        var currentMembership = currentUserId.HasValue
            ? group.Members.FirstOrDefault(x => x.UserId == currentUserId.Value)
            : null;
        var currentRole = currentMembership?.Role;
        var canManageMembers = currentRole is GroupMemberRole.Owner or GroupMemberRole.Moderator;
        var canManageSettings = currentRole == GroupMemberRole.Owner;
        var canCreateEvents = currentRole is GroupMemberRole.Owner or GroupMemberRole.Moderator;

        return new GroupDetailResponse
        {
            Id = summary.Id,
            Name = summary.Name,
            Slug = summary.Slug,
            ShortDescription = summary.ShortDescription,
            Description = summary.Description,
            AvatarUrl = summary.AvatarUrl,
            BannerUrl = summary.BannerUrl,
            Category = summary.Category,
            Rules = summary.Rules,
            CreatorUserId = summary.CreatorUserId,
            CreatorName = summary.CreatorName,
            MemberCount = summary.MemberCount,
            PreviewMembers = summary.PreviewMembers,
            JoinedByCurrentUser = summary.JoinedByCurrentUser,
            CreatedAtUtc = summary.CreatedAtUtc,
            PostCount = postCount,
            EventCount = eventCount,
            CanCurrentUserPost = summary.JoinedByCurrentUser,
            CurrentUserRole = currentRole,
            CanManageMembers = canManageMembers,
            CanManageSettings = canManageSettings,
            CanCreateEvents = canCreateEvents,
            ModeratorPreviewMembers = group.Members
                .Where(x => x.Role == GroupMemberRole.Owner || x.Role == GroupMemberRole.Moderator)
                .OrderByDescending(x => x.Role)
                .ThenByDescending(x => x.JoinedAtUtc)
                .Take(GroupModeratorPreviewLimit)
                .Select(x => x.ToPreviewResponse())
                .ToArray()
        };
    }

    public static GroupMemberPreviewResponse ToPreviewResponse(this GroupMember membership)
    {
        return new GroupMemberPreviewResponse
        {
            UserId = membership.UserId,
            FullName = membership.User.FullName,
            AvatarUrl = membership.User.StudentProfile?.AvatarUrl,
            Department = membership.User.StudentProfile?.Department,
            Role = membership.Role
        };
    }

    public static GroupMemberResponse ToResponse(
        this GroupMember membership,
        GroupMemberRole? currentUserRole,
        Guid? currentUserId)
    {
        var canManageMembers = currentUserRole is GroupMemberRole.Owner or GroupMemberRole.Moderator;
        var targetRole = membership.Role;
        var isCurrentUser = currentUserId.HasValue && membership.UserId == currentUserId.Value;
        var canPromote = currentUserRole == GroupMemberRole.Owner && targetRole == GroupMemberRole.Member && !isCurrentUser;
        var canDemote = currentUserRole == GroupMemberRole.Owner && targetRole == GroupMemberRole.Moderator && !isCurrentUser;
        var canRemove =
            canManageMembers &&
            !isCurrentUser &&
            targetRole != GroupMemberRole.Owner &&
            (currentUserRole == GroupMemberRole.Owner || targetRole == GroupMemberRole.Member);

        return new GroupMemberResponse
        {
            UserId = membership.UserId,
            FullName = membership.User.FullName,
            AvatarUrl = membership.User.StudentProfile?.AvatarUrl,
            Department = membership.User.StudentProfile?.Department,
            Role = membership.Role,
            JoinedAtUtc = membership.JoinedAtUtc,
            IsCurrentUser = isCurrentUser,
            CanBePromoted = canPromote,
            CanBeDemoted = canDemote,
            CanBeRemoved = canRemove,
        };
    }

    private static IReadOnlyCollection<string> ParseGroupRules(string? rulesJson)
    {
        if (string.IsNullOrWhiteSpace(rulesJson))
        {
            return [];
        }

        try
        {
            var rules = JsonSerializer.Deserialize<string[]>(rulesJson);
            return rules?
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static EventResponse ToResponse(this Event entity, Guid? currentUserId)
    {
        return new EventResponse
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Location = entity.Location,
            StartDateUtc = entity.StartDateUtc,
            EndDateUtc = entity.EndDateUtc,
            CreatorUserId = entity.CreatorUserId,
            CreatorName = entity.CreatorUser.FullName,
            GroupId = entity.GroupId,
            GroupName = entity.Group?.Name,
            MaxParticipants = entity.MaxParticipants,
            ParticipantCount = entity.Participants.Count(x => x.Status == Domain.Enums.EventParticipantStatus.Registered),
            RegisteredByCurrentUser = currentUserId.HasValue &&
                                      entity.Participants.Any(x =>
                                          x.UserId == currentUserId.Value &&
                                          x.Status == Domain.Enums.EventParticipantStatus.Registered),
            Category = entity.Category
        };
    }

    public static ProductResponse ToResponse(this Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name,
            SellerId = product.SellerId,
            SellerName = product.Seller.FullName,
            Condition = product.Condition,
            IsActive = product.IsActive,
            City = product.City,
            IsNegotiable = product.IsNegotiable,
            ImageUrls = product.Images
                .OrderBy(x => x.SortOrder)
                .Select(x => x.Url)
                .ToArray(),
            CreatedAtUtc = product.CreatedAtUtc
        };
    }

    public static DiscountResponse ToResponse(this Discount discount)
    {
        return new DiscountResponse
        {
            Id = discount.Id,
            BusinessName = discount.BusinessName,
            Title = discount.Title,
            Description = discount.Description,
            DiscountRate = discount.DiscountRate,
            DiscountCode = discount.DiscountCode,
            LogoUrl = discount.LogoUrl,
            ValidUntilUtc = discount.ValidUntilUtc,
            IsActive = discount.IsActive && discount.ValidUntilUtc > DateTime.UtcNow
        };
    }

    public static NotificationResponse ToResponse(this Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            IsRead = notification.IsRead,
            Type = notification.Type,
            CreatedAtUtc = notification.CreatedAtUtc,
            TargetPath = notification.TargetPath
        };
    }

    public static VisualSearchHistoryResponse ToResponse(this VisualSearchHistory history)
    {
        return new VisualSearchHistoryResponse
        {
            Id = history.Id,
            QueryImageUrl = history.QueryImageUrl,
            SearchedAtUtc = history.SearchedAtUtc,
            ResultCount = history.ResultCount,
            Results = history.Results
                .OrderBy(x => x.Rank)
                .Select(x => new VisualSearchResultResponse
                {
                    ProductId = x.ProductId,
                    Title = x.Product.Title,
                    Description = x.Product.Description,
                    Price = x.Product.Price,
                    ImageUrl = x.Product.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).FirstOrDefault(),
                    CategoryLabel = x.Product.Category?.Name ?? "Diger",
                    SellerName = x.Product.Seller.FullName,
                    Condition = x.Product.Condition,
                    ConditionLabel = x.Product.Condition switch
                    {
                        Domain.Enums.ProductCondition.New => "Sifir",
                        Domain.Enums.ProductCondition.LikeNew => "Yeni gibi",
                        Domain.Enums.ProductCondition.Good => "Iyi",
                        Domain.Enums.ProductCondition.Fair => "Orta",
                        _ => "Bilinmiyor"
                    },
                    City = x.Product.City,
                    SimilarityScore = x.SimilarityScore,
                    Rank = x.Rank,
                    MatchedSignals = [],
                    Breakdown = []
                })
                .ToArray()
        };
    }
}
