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
    IPostMediaStorageService postMediaStorageService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<PostResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = QueryPostsForFeed().OrderByDescending(x => x.CreatedAtUtc);
        var totalCount = await query.CountAsync(cancellationToken);

        var posts = await query
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

        var query = QueryBookmarkedPostsForUser(userId.Value);
        var totalCount = await query.CountAsync(cancellationToken);

        var bookmarks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<PostResponse>
        {
            Items = bookmarks.Select(x => x.Post.ToResponse(userId.Value)).ToArray(),
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
        if (content.Length == 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Content)] = ["Icerik bos olamaz."]
            }));
        }

        if (image is not null && !string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            return BadRequest(new
            {
                message = "Ayni istekte hem gorsel dosyasi hem de imageUrl gonderemezsiniz."
            });
        }

        string? imageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();

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
                .AnyAsync(x => x.Id == request.GroupId.Value, cancellationToken);

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
        [FromBody] UpdatePostRequest request,
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
        if (content.Length == 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Content)] = ["Icerik bos olamaz."]
            }));
        }

        post.Content = content;
        post.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();

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
            dbContext.PostLikes.Add(new PostLike
            {
                PostId = post.Id,
                UserId = userId.Value
            });

            if (post.UserId != userId.Value)
            {
                dbContext.Notifications.Add(new Notification
                {
                    UserId = post.UserId,
                    Title = $"{actorName} gonderini begendi",
                    Message = BuildNotificationExcerpt(post.Content, "Gonderine yeni bir begeni geldi."),
                    Type = NotificationType.Social,
                    TargetPath = BuildPostTargetPath(post.Id)
                });
            }
        }
        else
        {
            dbContext.PostLikes.Remove(like);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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

        if (post.UserId != userId.Value)
        {
            dbContext.Notifications.Add(new Notification
            {
                UserId = post.UserId,
                Title = $"{actorName} gonderine yorum yapti",
                Message = BuildNotificationExcerpt(content, "Gonderine yeni bir yorum geldi."),
                Type = NotificationType.Social,
                TargetPath = BuildPostTargetPath(post.Id)
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        comment = await dbContext.PostComments
            .AsNoTracking()
            .Include(x => x.User)
            .ThenInclude(x => x.StudentProfile)
            .FirstAsync(x => x.Id == comment.Id, cancellationToken);

        return Ok(comment.ToResponse());
    }

    private IQueryable<Post> QueryPostsForFeed()
    {
        return dbContext.Posts
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => !x.IsDeleted)
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

    private IQueryable<PostBookmark> QueryBookmarkedPostsForUser(Guid userId)
    {
        return dbContext.PostBookmarks
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => x.UserId == userId && !x.Post.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Include(x => x.Post)
            .ThenInclude(x => x.User)
            .ThenInclude(x => x.StudentProfile)
            .Include(x => x.Post)
            .ThenInclude(x => x.Group)
            .Include(x => x.Post)
            .ThenInclude(x => x.Likes)
            .Include(x => x.Post)
            .ThenInclude(x => x.Comments)
            .Include(x => x.Post)
            .ThenInclude(x => x.Bookmarks)
            .Include(x => x.Post)
            .ThenInclude(x => x.Views);
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
}
