using System.Text;
using EduConnect.Api.Mappings;
using EduConnect.Application.Contracts.Common;
using EduConnect.Application.Contracts.Groups;
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
public sealed class GroupsController(AppDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    private const int ShortDescriptionMaxLength = 220;
    private const int DefaultJoinedGroupsLimit = 12;
    private const int DefaultDiscoverGroupsLimit = 12;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<GroupResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var groups = await QueryGroups()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(groups.Select(x => x.ToResponse(currentUserService.UserId)).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<GroupResponse>> Create([FromBody] CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var group = new Group
        {
            Name = request.Name.Trim(),
            Slug = await GenerateUniqueSlugAsync(request.Name, cancellationToken),
            ShortDescription = BuildShortDescription(request),
            Description = request.Description.Trim(),
            AvatarUrl = NormalizeOptionalUrl(request.AvatarUrl),
            BannerUrl = NormalizeOptionalUrl(request.BannerUrl),
            Category = request.Category.Trim(),
            CreatorUserId = userId.Value
        };

        dbContext.Groups.Add(group);
        dbContext.GroupMembers.Add(new GroupMember
        {
            Group = group,
            UserId = userId.Value,
            Role = GroupMemberRole.Owner
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        group = await QueryGroups().FirstAsync(x => x.Id == group.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group.ToResponse(userId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GroupDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var group = await QueryGroups()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (group is null)
        {
            return NotFound();
        }

        return Ok(await BuildGroupDetailResponseAsync(group, cancellationToken));
    }

    [HttpGet("joined")]
    public async Task<ActionResult<IReadOnlyCollection<GroupResponse>>> GetJoined(
        [FromQuery] int limit = DefaultJoinedGroupsLimit,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        limit = Math.Clamp(limit, 1, 24);

        var groups = await QueryGroups()
            .Where(x => x.Members.Any(member => member.UserId == userId.Value))
            .OrderByDescending(x => x.Members
                .Where(member => member.UserId == userId.Value)
                .Select(member => member.JoinedAtUtc)
                .FirstOrDefault())
            .Take(limit)
            .ToListAsync(cancellationToken);

        return Ok(groups.Select(x => x.ToResponse(userId)).ToArray());
    }

    [HttpGet("discover")]
    public async Task<ActionResult<IReadOnlyCollection<GroupResponse>>> GetDiscover(
        [FromQuery] string? query,
        [FromQuery] int limit = DefaultDiscoverGroupsLimit,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        limit = Math.Clamp(limit, 1, 24);

        var discoverQuery = QueryGroups()
            .Where(x => !x.Members.Any(member => member.UserId == userId.Value));

        var normalizedQuery = query?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            discoverQuery = discoverQuery.Where(x =>
                x.Name.Contains(normalizedQuery) ||
                x.ShortDescription.Contains(normalizedQuery) ||
                x.Description.Contains(normalizedQuery) ||
                x.Category.Contains(normalizedQuery));
        }

        var groups = await discoverQuery
            .OrderByDescending(x => x.Members.Count)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return Ok(groups.Select(x => x.ToResponse(userId)).ToArray());
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<GroupDetailResponse>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (normalizedSlug.Length == 0)
        {
            return NotFound();
        }

        var group = await QueryGroups()
            .FirstOrDefaultAsync(x => x.Slug == normalizedSlug, cancellationToken);

        if (group is null)
        {
            return NotFound();
        }

        return Ok(await BuildGroupDetailResponseAsync(group, cancellationToken));
    }

    [HttpGet("{id:guid}/posts")]
    public async Task<ActionResult<PagedResponse<PostResponse>>> GetPosts(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var groupExists = await dbContext.Groups
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);

        if (!groupExists)
        {
            return NotFound();
        }

        var query = QueryGroupPosts()
            .Where(x => x.GroupId == id)
            .OrderByDescending(x => x.CreatedAtUtc);

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

    [HttpGet("feed")]
    public async Task<ActionResult<PagedResponse<PostResponse>>> GetFeed(
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

        var query = QueryGroupPosts()
            .Where(x => x.GroupId.HasValue && x.Group!.Members.Any(member => member.UserId == userId.Value))
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

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var actorName = await GetCurrentUserDisplayNameAsync(userId.Value, cancellationToken);

        var group = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (group is null)
        {
            return NotFound();
        }

        var alreadyMember = await dbContext.GroupMembers.AnyAsync(
            x => x.GroupId == id && x.UserId == userId.Value,
            cancellationToken);

        if (alreadyMember)
        {
            return BadRequest(new { message = "Kullanici zaten bu grupta." });
        }

        dbContext.GroupMembers.Add(new GroupMember
        {
            GroupId = id,
            UserId = userId.Value
        });

        if (group.CreatorUserId != userId.Value)
        {
            dbContext.Notifications.Add(new Notification
            {
                UserId = group.CreatorUserId,
                Title = $"{actorName} grubuna katildi",
                Message = $"\"{group.Name}\" topluluguna yeni bir uye eklendi.",
                Type = NotificationType.Social,
                TargetPath = "/communities"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var membership = await dbContext.GroupMembers
            .FirstOrDefaultAsync(x => x.GroupId == id && x.UserId == userId.Value, cancellationToken);

        if (membership is null)
        {
            return NotFound();
        }

        if (membership.Role == GroupMemberRole.Owner)
        {
            return BadRequest(new { message = "Grup sahibi kendi olusturdugu gruptan ayrilamaz." });
        }

        dbContext.GroupMembers.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<Group> QueryGroups()
    {
        return dbContext.Groups
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.CreatorUser)
            .Include(x => x.Members)
            .ThenInclude(x => x.User)
            .ThenInclude(x => x.StudentProfile);
    }

    private IQueryable<Post> QueryGroupPosts()
    {
        return dbContext.Posts
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => !x.IsDeleted && x.GroupId.HasValue)
            .Include(x => x.User)
            .ThenInclude(x => x.StudentProfile)
            .Include(x => x.Group)
            .Include(x => x.Likes)
            .Include(x => x.Comments)
            .Include(x => x.Bookmarks)
            .Include(x => x.Views);
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var slugBase = BuildSlugBase(name);
        var slug = slugBase;
        var suffix = 2;

        while (await dbContext.Groups.AnyAsync(x => x.Slug == slug, cancellationToken))
        {
            slug = $"{slugBase}-{suffix}";
            suffix += 1;
        }

        return slug;
    }

    private static string BuildShortDescription(CreateGroupRequest request)
    {
        var preferredValue = string.IsNullOrWhiteSpace(request.ShortDescription)
            ? request.Description
            : request.ShortDescription;

        var normalized = preferredValue.Trim();
        if (normalized.Length <= ShortDescriptionMaxLength)
        {
            return normalized;
        }

        return normalized[..ShortDescriptionMaxLength].TrimEnd();
    }

    private static string? NormalizeOptionalUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildSlugBase(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);
        var previousWasSeparator = false;

        foreach (var character in normalized)
        {
            var nextCharacter = character switch
            {
                '\u00E7' => 'c',
                '\u011F' => 'g',
                '\u0131' => 'i',
                '\u00F6' => 'o',
                '\u015F' => 's',
                '\u00FC' => 'u',
                _ => character
            };

            if (char.IsLetterOrDigit(nextCharacter))
            {
                builder.Append(nextCharacter);
                previousWasSeparator = false;
                continue;
            }

            if (previousWasSeparator)
            {
                continue;
            }

            builder.Append('-');
            previousWasSeparator = true;
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "community" : slug;
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

    private async Task<GroupDetailResponse> BuildGroupDetailResponseAsync(
        Group group,
        CancellationToken cancellationToken)
    {
        var postCount = await dbContext.Posts
            .AsNoTracking()
            .CountAsync(x => x.GroupId == group.Id && !x.IsDeleted, cancellationToken);

        var eventCount = await dbContext.Events
            .AsNoTracking()
            .CountAsync(x => x.GroupId == group.Id, cancellationToken);

        return group.ToDetailResponse(currentUserService.UserId, postCount, eventCount);
    }
}
