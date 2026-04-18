using EduConnect.Api.Common;
using EduConnect.Api.Mappings;
using EduConnect.Application.Contracts.Common;
using EduConnect.Application.Contracts.Posts;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Domain.Enums;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class PostsController(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    IPostMediaStorageService postMediaStorageService,
    NotificationPublisher notificationPublisher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<PostResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? userId = null,
        [FromQuery] bool mediaOnly = false,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = QueryPostsForFeed();
        if (userId.HasValue) query = query.Where(x => x.UserId == userId.Value);
        if (mediaOnly) query = query.Where(x => x.ImageUrl != null);

        var ordered = query.OrderByDescending(x => x.CreatedAtUtc);
        var totalCount = await ordered.CountAsync(cancellationToken);

        var posts = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<PostResponse>
        {
            Items = posts.Select(x => x.ToResponse(currentUserService.UserId)).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet("for-you")]
    public async Task<ActionResult<PagedResponse<PostResponse>>> GetForYou(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var currentUserUniversityId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId.Value)
            .Select(x => x.UniversityId)
            .FirstOrDefaultAsync(cancellationToken);

        var followedUserIds = await dbContext.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowerUserId == userId.Value)
            .Select(x => x.FollowedUserId)
            .ToArrayAsync(cancellationToken);

        var joinedGroupIds = await dbContext.GroupMembers
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value)
            .Select(x => x.GroupId)
            .ToArrayAsync(cancellationToken);

        var interestSinceUtc = DateTime.UtcNow.AddDays(-45);
        var interestPosts = await dbContext.Posts
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.CreatedAtUtc >= interestSinceUtc &&
                (x.UserId == userId.Value ||
                 x.Likes.Any(like => like.UserId == userId.Value) ||
                 x.Bookmarks.Any(bookmark => bookmark.UserId == userId.Value)))
            .Select(x => new PostTrendSource(
                x.Content,
                x.UserId,
                x.User.UniversityId,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var interestHashtags = PostTrendAnalyzer.Build(interestPosts, 6)
            .Select(x => x.DisplayHashtag.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidateSinceUtc = DateTime.UtcNow.AddDays(-30);
        var candidatePosts = await QueryPostsForFeed()
            .Where(x => x.CreatedAtUtc >= candidateSinceUtc)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken);

        var scoredPosts = candidatePosts
            .Select(post => new ScoredPost(
                Post: post,
                Score: CalculateForYouScore(
                    post,
                    userId.Value,
                    currentUserUniversityId,
                    followedUserIds,
                    joinedGroupIds,
                    interestHashtags)))
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Post.CreatedAtUtc)
            .ToArray();

        var totalCount = scoredPosts.Length;
        var posts = scoredPosts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Post.ToResponse(
                userId,
                BuildForYouReason(
                    x.Post,
                    currentUserId: userId.Value,
                    currentUserUniversityId,
                    followedUserIds,
                    joinedGroupIds,
                    interestHashtags)))
            .ToArray();

        return Ok(new PagedResponse<PostResponse>
        {
            Items = posts,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet("following")]
    public async Task<ActionResult<PagedResponse<PostResponse>>> GetFollowingFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var followedUserIdList = await dbContext.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowerUserId == userId.Value)
            .Select(x => x.FollowedUserId)
            .ToArrayAsync(cancellationToken);

        if (followedUserIdList.Length == 0)
        {
            return Ok(new PagedResponse<PostResponse>
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            });
        }

        var query = QueryPostsForFeed()
            .Where(x => !x.GroupId.HasValue && followedUserIdList.Contains(x.UserId))
            .OrderByDescending(x => x.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<PostResponse>
        {
            Items = posts.Select(x => x.ToResponse(userId)).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet("trending")]
    public async Task<ActionResult<IReadOnlyCollection<PostTrendingHashtagResponse>>> GetTrendingHashtags(
        [FromQuery] int limit = 4,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        limit = Math.Clamp(limit, 1, 10);

        var currentUserUniversityId = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId.Value)
            .Select(x => x.UniversityId)
            .FirstOrDefaultAsync(cancellationToken);

        var sinceUtc = DateTime.UtcNow.AddDays(-7);

        var recentPosts = await dbContext.Posts
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.CreatedAtUtc >= sinceUtc && x.Content.Contains("#"))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new TrendingPostSource(
                x.Content,
                x.UserId,
                x.User.UniversityId,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<PostTrendingHashtagResponse> trends;

        if (currentUserUniversityId.HasValue)
        {
            trends = BuildTrendingHashtagResponses(
                recentPosts.Where(x => x.UniversityId == currentUserUniversityId.Value),
                "Universitende - Gundemdekiler",
                limit);

            if (trends.Count == 0)
            {
                trends = BuildTrendingHashtagResponses(
                    recentPosts,
                    "Platformda - Gundemdekiler",
                    limit);
            }
        }
        else
        {
            trends = BuildTrendingHashtagResponses(
                recentPosts,
                "Platformda - Gundemdekiler",
                limit);
        }

        return Ok(trends);
    }

    [HttpGet("tags/{tag}")]
    public async Task<ActionResult<PagedResponse<PostResponse>>> GetPostsByTag(
        string tag,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedTag = NormalizeHashtagTag(tag);
        if (normalizedTag is null)
        {
            return BadRequest(new
            {
                message = "Gecersiz hashtag."
            });
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var matchingPosts = await QueryPostsForFeed()
            .Where(x => x.Content.Contains("#"))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var filteredPosts = matchingPosts
            .Where(post => PostTrendAnalyzer.ExtractHashtags(post.Content)
                .Any(hashtag => string.Equals(
                    hashtag.TrimStart('#'),
                    normalizedTag,
                    StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        var pagedPosts = filteredPosts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.ToResponse(currentUserService.UserId))
            .ToArray();

        return Ok(new PagedResponse<PostResponse>
        {
            Items = pagedPosts,
            Page = page,
            PageSize = pageSize,
            TotalCount = filteredPosts.Length
        });
    }

    [HttpGet("bookmarks")]
    public async Task<ActionResult<PagedResponse<PostResponse>>> GetBookmarked(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var bookmarkedQuery = QueryPostsForFeed()
            .Where(p => p.Bookmarks.Any(b => b.UserId == userId.Value));

        var totalCount = await bookmarkedQuery.CountAsync(cancellationToken);

        var posts = await bookmarkedQuery
            .OrderByDescending(p => p.Bookmarks.Where(b => b.UserId == userId.Value).Select(b => b.CreatedAtUtc).FirstOrDefault())
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<PostResponse>
        {
            Items = posts.Select(x => x.ToResponse(userId.Value)).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet("liked")]
    public async Task<ActionResult<PagedResponse<PostResponse>>> GetLikedPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = currentUserService.UserId;
        if (currentUserId is null) return Unauthorized();

        var targetUserId = userId ?? currentUserId.Value;
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var likedQuery = QueryPostsForFeed()
            .Where(p => p.Likes.Any(l => l.UserId == targetUserId));

        var totalCount = await likedQuery.CountAsync(cancellationToken);

        var posts = await likedQuery
            .OrderByDescending(p => p.Likes.Where(l => l.UserId == targetUserId).Select(l => l.CreatedAtUtc).FirstOrDefault())
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<PostResponse>
        {
            Items = posts.Select(x => x.ToResponse(currentUserId)).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpPost]
    public async Task<ActionResult<PostResponse>> Create(
        [FromForm] CreatePostRequest request,
        [FromForm(Name = "image")] IFormFile? image,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var content = request.Content.Trim();

        string? imageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();

        if (content.Length == 0 && image is null && imageUrl is null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Content)] = ["Bir metin yaz veya gorsel ekle."]
            }));
        }

        if (image is not null)
        {
            await using var fileStream = image.OpenReadStream();
            imageUrl = await postMediaStorageService.SaveAsync(
                userId.Value,
                fileStream,
                image.FileName,
                image.ContentType,
                cancellationToken);
        }

        if (request.GroupId.HasValue)
        {
            var groupExists = await dbContext.Groups
                .AnyAsync(x => x.Id == request.GroupId.Value && x.IsActive, cancellationToken);

            if (!groupExists)
            {
                return NotFound(new
                {
                    message = "Topluluk bulunamadi."
                });
            }

            var isMember = await dbContext.GroupMembers
                .AnyAsync(
                    x => x.GroupId == request.GroupId.Value && x.UserId == userId.Value,
                    cancellationToken);

            if (!isMember)
            {
                return Forbid();
            }
        }

        var post = new Post
        {
            UserId = userId.Value,
            GroupId = request.GroupId,
            Content = content,
            ImageUrl = imageUrl
        };

        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);

        post = await QueryPostsForFeed().FirstAsync(x => x.Id == post.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post.ToResponse(userId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var post = await QueryPostsWithDetails()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (post is null)
        {
            return NotFound();
        }

        return Ok(new PostDetailResponse
        {
            Post = post.ToResponse(currentUserService.UserId),
            Comments = post.Comments
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => x.ToResponse())
                .ToArray()
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PostResponse>> Update(
        Guid id,
        [FromForm] UpdatePostRequest request,
        [FromForm(Name = "image")] IFormFile? image,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var post = await dbContext.Posts
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (post is null)
        {
            return NotFound();
        }

        if (!CanManagePost(post, currentUserId.Value))
        {
            return Forbid();
        }

        var content = request.Content.Trim();
        if (content.Length == 0 && image is null && post.ImageUrl is null && !request.RemoveImage)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Content)] = ["Bir metin yaz veya gorsel ekle."]
            }));
        }

        post.Content = content;

        if (request.RemoveImage)
        {
            post.ImageUrl = null;
        }
        else if (image is not null)
        {
            await using var fileStream = image.OpenReadStream();
            post.ImageUrl = await postMediaStorageService.SaveAsync(
                currentUserId.Value,
                fileStream,
                image.FileName,
                image.ContentType,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var updatedPost = await QueryPostsForFeed()
            .FirstAsync(x => x.Id == post.Id, cancellationToken);

        return Ok(updatedPost.ToResponse(currentUserId));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var post = await dbContext.Posts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        var currentUserId = currentUserService.UserId;
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        if (!CanManagePost(post, currentUserId.Value))
        {
            return Forbid();
        }

        post.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/like")]
    public async Task<IActionResult> ToggleLike(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var actorName = await GetCurrentUserDisplayNameAsync(userId.Value, cancellationToken);

        var post = await dbContext.Posts
            .Include(x => x.Likes)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (post is null)
        {
            return NotFound();
        }

        var like = post.Likes.FirstOrDefault(x => x.UserId == userId.Value);
        if (like is null)
        {
            Notification? createdNotification = null;

            dbContext.PostLikes.Add(new PostLike
            {
                PostId = post.Id,
                UserId = userId.Value
            });

            if (post.UserId != userId.Value)
            {
                createdNotification = new Notification
                {
                    UserId = post.UserId,
                    Title = $"{actorName} gonderini begendi",
                    Message = BuildNotificationExcerpt(post.Content, "Gonderine yeni bir begeni geldi."),
                    Type = NotificationType.Social,
                    TargetPath = BuildPostTargetPath(post.Id)
                };
                dbContext.Notifications.Add(createdNotification);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            if (createdNotification is not null)
            {
                await notificationPublisher.PublishAsync(createdNotification, cancellationToken);
            }
        }
        else
        {
            dbContext.PostLikes.Remove(like);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }

    [HttpPost("{id:guid}/bookmark")]
    public async Task<ActionResult<PostBookmarkStateResponse>> ToggleBookmark(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var post = await dbContext.Posts
            .Include(x => x.Bookmarks)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (post is null)
        {
            return NotFound();
        }

        var bookmark = post.Bookmarks.FirstOrDefault(x => x.UserId == userId.Value);
        var isBookmarked = bookmark is null;

        if (bookmark is null)
        {
            dbContext.PostBookmarks.Add(new PostBookmark
            {
                PostId = post.Id,
                UserId = userId.Value
            });
        }
        else
        {
            dbContext.PostBookmarks.Remove(bookmark);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new PostBookmarkStateResponse
        {
            IsBookmarked = isBookmarked
        });
    }

    [HttpPost("{id:guid}/view")]
    public async Task<ActionResult<PostViewTrackingResponse>> TrackView(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var post = await dbContext.Posts
            .Include(x => x.Views)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (post is null)
        {
            return NotFound();
        }

        var existingView = post.Views.FirstOrDefault(x => x.UserId == userId.Value);
        var viewsCount = post.Views.Count;

        if (existingView is null)
        {
            dbContext.PostViews.Add(new PostView
            {
                PostId = post.Id,
                UserId = userId.Value
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            viewsCount += 1;
        }

        return Ok(new PostViewTrackingResponse
        {
            ViewsCount = viewsCount
        });
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<IReadOnlyCollection<PostCommentResponse>>> GetComments(Guid id, CancellationToken cancellationToken)
    {
        var comments = await dbContext.PostComments
            .AsNoTracking()
            .Include(x => x.User)
            .ThenInclude(x => x.StudentProfile)
            .Where(x => x.PostId == id)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(comments.Select(x => x.ToResponse()).ToArray());
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<PostCommentResponse>> AddComment(
        Guid id,
        [FromBody] CreatePostCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var post = await dbContext.Posts
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (post is null)
        {
            return NotFound();
        }

        var content = request.Content.Trim();
        if (content.Length == 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Content)] = ["Yorum bos olamaz."]
            }));
        }

        var actorName = await GetCurrentUserDisplayNameAsync(userId.Value, cancellationToken);

        var comment = new PostComment
        {
            PostId = post.Id,
            UserId = userId.Value,
            Content = content
        };

        dbContext.PostComments.Add(comment);

        Notification? createdNotification = null;

        if (post.UserId != userId.Value)
        {
            createdNotification = new Notification
            {
                UserId = post.UserId,
                Title = $"{actorName} gonderine yorum yapti",
                Message = BuildNotificationExcerpt(content, "Gonderine yeni bir yorum geldi."),
                Type = NotificationType.Social,
                TargetPath = BuildPostTargetPath(post.Id)
            };
            dbContext.Notifications.Add(createdNotification);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (createdNotification is not null)
        {
            await notificationPublisher.PublishAsync(createdNotification, cancellationToken);
        }

        comment = await dbContext.PostComments
            .AsNoTracking()
            .Include(x => x.User)
            .ThenInclude(x => x.StudentProfile)
            .FirstAsync(x => x.Id == comment.Id, cancellationToken);

        return Ok(comment.ToResponse());
    }

    [HttpDelete("{postId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(
        Guid postId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var comment = await dbContext.PostComments
            .Include(x => x.Post)
            .FirstOrDefaultAsync(
                x => x.Id == commentId && x.PostId == postId,
                cancellationToken);

        if (comment is null)
        {
            return NotFound();
        }

        var isAuthor = comment.UserId == userId.Value;
        var isPostOwner = comment.Post.UserId == userId.Value;
        var currentUserRole = await dbContext.Users
            .Where(x => x.Id == userId.Value)
            .Select(x => x.Role)
            .FirstOrDefaultAsync(cancellationToken);
        var isAdmin = currentUserRole is UserRole.Admin or UserRole.Moderator;

        if (!isAuthor && !isPostOwner && !isAdmin)
        {
            return Forbid();
        }

        dbContext.PostComments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private IQueryable<Post> QueryPostsForFeed()
    {
        return dbContext.Posts
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => !x.IsDeleted && (!x.GroupId.HasValue || (x.Group != null && x.Group.IsActive)))
            .Include(x => x.User)
            .ThenInclude(x => x.StudentProfile)
            .Include(x => x.Group)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .Include(x => x.Bookmarks)
            .Include(x => x.Views);
    }

    private IQueryable<Post> QueryPostsWithDetails()
    {
        return QueryPostsForFeed()
            .Include(x => x.Comments)
            .ThenInclude(x => x.User)
            .ThenInclude(x => x.StudentProfile);
    }

private static IReadOnlyCollection<PostTrendingHashtagResponse> BuildTrendingHashtagResponses(
        IEnumerable<TrendingPostSource> posts,
        string contextLabel,
        int limit)
    {
        return PostTrendAnalyzer.Build(
                posts.Select(post => new PostTrendSource(
                    post.Content,
                    post.UserId,
                    post.UniversityId,
                    post.CreatedAtUtc)),
                limit)
            .Select(x => new PostTrendingHashtagResponse
            {
                ContextLabel = contextLabel,
                Hashtag = x.DisplayHashtag,
                PostCount = x.PostCount,
                UniqueAuthorCount = x.UniqueAuthorCount
            })
            .ToArray();
    }

    private static int CalculateForYouScore(
        Post post,
        Guid currentUserId,
        Guid? currentUserUniversityId,
        IReadOnlyCollection<Guid> followedUserIds,
        IReadOnlyCollection<Guid> joinedGroupIds,
        IReadOnlySet<string> interestHashtags)
    {
        var score = 0;

        if (followedUserIds.Contains(post.UserId))
        {
            score += 46;
        }

        if (post.GroupId.HasValue && joinedGroupIds.Contains(post.GroupId.Value))
        {
            score += 34;
        }

        if (currentUserUniversityId.HasValue && post.User.UniversityId == currentUserUniversityId.Value)
        {
            score += 18;
        }

        score += post.GroupId.HasValue ? 4 : 8;
        score += Math.Min(post.Likes.Count, 12);
        score += Math.Min(post.Comments.Count * 2, 12);
        score += Math.Min(post.Views.Count / 3, 10);

        if (post.Likes.Any(x => x.UserId == currentUserId) || post.Bookmarks.Any(x => x.UserId == currentUserId))
        {
            score += 8;
        }

        if (interestHashtags.Count > 0)
        {
            var matchingHashtagCount = PostTrendAnalyzer.ExtractHashtags(post.Content)
                .Count(hashtag => interestHashtags.Contains(hashtag.ToLowerInvariant()));

            if (matchingHashtagCount > 0)
            {
                score += 18 + (matchingHashtagCount * 6);
            }
        }

        var ageHours = Math.Max(0, (DateTime.UtcNow - post.CreatedAtUtc).TotalHours);
        score += ageHours switch
        {
            <= 12 => 14,
            <= 24 => 10,
            <= 72 => 6,
            <= 168 => 3,
            _ => 0
        };

        return score;
    }

    private static string BuildForYouReason(
        Post post,
        Guid currentUserId,
        Guid? currentUserUniversityId,
        IReadOnlyCollection<Guid> followedUserIds,
        IReadOnlyCollection<Guid> joinedGroupIds,
        IReadOnlySet<string> interestHashtags)
    {
        if (followedUserIds.Contains(post.UserId))
        {
            return "Takip ettiklerinden";
        }

        if (post.GroupId.HasValue && joinedGroupIds.Contains(post.GroupId.Value))
        {
            return "Toplulugundan";
        }

        if (interestHashtags.Count > 0)
        {
            var hasMatchingHashtag = PostTrendAnalyzer.ExtractHashtags(post.Content)
                .Any(hashtag => interestHashtags.Contains(hashtag.ToLowerInvariant()));

            if (hasMatchingHashtag)
            {
                return "Ilgi alanina yakin";
            }
        }

        if (currentUserUniversityId.HasValue && post.User.UniversityId == currentUserUniversityId.Value)
        {
            return "Ayni universiteden";
        }

        var engagementScore = post.Likes.Count + post.Comments.Count + post.Views.Count;
        if (engagementScore >= 8)
        {
            return "Etkilesimi yuksek";
        }

        return "Yeni paylasildi";
    }

    private bool CanManagePost(Post post, Guid currentUserId)
    {
        var isPrivileged =
            User.IsInRole(UserRole.Admin.ToString()) ||
            User.IsInRole(UserRole.Moderator.ToString());

        return isPrivileged || post.UserId == currentUserId;
    }

    private async Task<string> GetCurrentUserDisplayNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.FullName)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Bir kullanici";
    }

    private static string BuildPostTargetPath(Guid postId)
    {
        return $"/post/{postId}";
    }

    private static string BuildNotificationExcerpt(string value, string fallback)
    {
        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            return fallback;
        }

        return normalized.Length <= 140
            ? normalized
            : $"{normalized[..140].TrimEnd()}...";
    }

    private static string? NormalizeHashtagTag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedValue = value.Trim().TrimStart('#');
        if (trimmedValue.Length is < 2 or > 40)
        {
            return null;
        }

        var normalizedValue = new string(trimmedValue
            .Where(character => char.IsLetterOrDigit(character) || character == '_')
            .ToArray());

        return normalizedValue.Length == trimmedValue.Length
            ? normalizedValue
            : null;
    }

    private sealed record TrendingPostSource(
        string Content,
        Guid UserId,
        Guid? UniversityId,
        DateTime CreatedAtUtc);

    private sealed record ScoredPost(
        Post Post,
        int Score);
}
