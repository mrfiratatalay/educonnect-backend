using EduConnect.Application.Contracts.Discounts;
using EduConnect.Application.Contracts.Events;
using EduConnect.Application.Contracts.Groups;
using EduConnect.Application.Contracts.Notifications;
using EduConnect.Application.Contracts.Posts;
using EduConnect.Application.Contracts.Products;
using EduConnect.Application.Contracts.Users;
using EduConnect.Application.Contracts.VisualSearch;
using EduConnect.Domain.Entities;

namespace EduConnect.Api.Mappings;

public static class MappingExtensions
{
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
            UniversityId = user.UniversityId,
            UniversityName = user.University?.Name
        };
    }

    public static PostResponse ToResponse(this Post post, Guid? currentUserId)
    {
        return new PostResponse
        {
            Id = post.Id,
            UserId = post.UserId,
            UserName = post.User.FullName,
            AvatarUrl = post.User.StudentProfile?.AvatarUrl,
            Content = post.Content,
            ImageUrl = post.ImageUrl,
            LikesCount = post.Likes.Count,
            CommentsCount = post.Comments.Count,
            LikedByCurrentUser = currentUserId.HasValue && post.Likes.Any(x => x.UserId == currentUserId.Value),
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
            Description = group.Description,
            Category = group.Category,
            CreatorUserId = group.CreatorUserId,
            CreatorName = group.CreatorUser.FullName,
            MemberCount = group.Members.Count,
            JoinedByCurrentUser = currentUserId.HasValue && group.Members.Any(x => x.UserId == currentUserId.Value),
            CreatedAtUtc = group.CreatedAtUtc
        };
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
            CreatedAtUtc = notification.CreatedAtUtc
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
                    Price = x.Product.Price,
                    ImageUrl = x.Product.Images.OrderBy(img => img.SortOrder).Select(img => img.Url).FirstOrDefault(),
                    SimilarityScore = x.SimilarityScore,
                    Rank = x.Rank
                })
                .ToArray()
        };
    }
}
