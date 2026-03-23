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
public sealed class PostsController(AppDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<PostResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = QueryPosts().OrderByDescending(x => x.CreatedAtUtc);
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

    [HttpPost]
    public async Task<ActionResult<PostResponse>> Create([FromBody] CreatePostRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var post = new Post
        {
            UserId = userId.Value,
            Content = request.Content.Trim(),
            ImageUrl = request.ImageUrl?.Trim()
        };

        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync(cancellationToken);

        post = await QueryPosts().FirstAsync(x => x.Id == post.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post.ToResponse(userId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var post = await QueryPosts()
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

        var isPrivileged = User.IsInRole(UserRole.Admin.ToString()) || User.IsInRole(UserRole.Moderator.ToString());
        if (!isPrivileged && post.UserId != currentUserId.Value)
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
                    Title = "Yeni beğeni",
                    Message = "Gönderiniz beğenildi.",
                    Type = NotificationType.Social
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

        var comment = new PostComment
        {
            PostId = post.Id,
            UserId = userId.Value,
            Content = request.Content.Trim()
        };

        dbContext.PostComments.Add(comment);

        if (post.UserId != userId.Value)
        {
            dbContext.Notifications.Add(new Notification
            {
                UserId = post.UserId,
                Title = "Yeni yorum",
                Message = "Gönderinize yeni bir yorum yapıldı.",
                Type = NotificationType.Social
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

    private IQueryable<Post> QueryPosts()
    {
        return dbContext.Posts
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => !x.IsDeleted)
            .Include(x => x.User)
            .ThenInclude(x => x.StudentProfile)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .ThenInclude(x => x.User)
            .ThenInclude(x => x.StudentProfile);
    }
}
