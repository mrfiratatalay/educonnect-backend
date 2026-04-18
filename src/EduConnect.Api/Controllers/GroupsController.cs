using System.Text;
using System.Text.Json;
using EduConnect.Api.Common;
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
public sealed class GroupsController(
    AppDbContext dbContext,
    ICurrentUserService currentUserService,
    NotificationPublisher notificationPublisher) : ControllerBase
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

    [HttpPost("upload-avatar")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadImageResponse>> UploadAvatar(
        [FromForm(Name = "file")] IFormFile file,
        [FromServices] IUserAvatarStorageService storageService,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();
        if (file is null) return BadRequest(new { message = "Dosya bulunamadi." });

        await using var stream = file.OpenReadStream();
        var url = await storageService.SaveAvatarAsync(userId.Value, stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(new UploadImageResponse(url));
    }

    [HttpPost("upload-banner")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadImageResponse>> UploadBanner(
        [FromForm(Name = "file")] IFormFile file,
        [FromServices] IUserAvatarStorageService storageService,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null) return Unauthorized();
        if (file is null) return BadRequest(new { message = "Dosya bulunamadi." });

        await using var stream = file.OpenReadStream();
        var url = await storageService.SaveCoverAsync(userId.Value, stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(new UploadImageResponse(url));
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
            ShortDescription = BuildShortDescription(request.ShortDescription, request.Description),
            Description = request.Description.Trim(),
            RulesJson = SerializeRules(request.Rules),
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

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GroupDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var group = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (group is null)
        {
            return NotFound();
        }

        if (group.CreatorUserId != userId.Value)
        {
            return Forbid();
        }

        group.Name = request.Name.Trim();
        group.ShortDescription = BuildShortDescription(request.ShortDescription, request.Description);
        group.Description = request.Description.Trim();
        group.RulesJson = SerializeRules(request.Rules);
        group.AvatarUrl = NormalizeOptionalUrl(request.AvatarUrl);
        group.BannerUrl = NormalizeOptionalUrl(request.BannerUrl);
        group.Category = request.Category.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        var updatedGroup = await QueryGroups().FirstAsync(x => x.Id == group.Id, cancellationToken);
        return Ok(await BuildGroupDetailResponseAsync(updatedGroup, cancellationToken));
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var group = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (group is null)
        {
            return NotFound();
        }

        if (group.CreatorUserId != userId.Value)
        {
            return Forbid();
        }

        group.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyCollection<GroupMemberResponse>>> GetMembers(
        Guid id,
        CancellationToken cancellationToken)
    {
        var group = await QueryGroups()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (group is null)
        {
            return NotFound();
        }

        var currentUserRole = ResolveCurrentUserRole(group, currentUserService.UserId);
        var members = group.Members
            .OrderByDescending(x => x.Role)
            .ThenBy(x => x.User.FullName)
            .Select(x => x.ToResponse(currentUserRole, currentUserService.UserId))
            .ToArray();

        return Ok(members);
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

        var group = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
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

        Notification? createdNotification = null;

        if (group.CreatorUserId != userId.Value)
        {
            createdNotification = new Notification
            {
                UserId = group.CreatorUserId,
                Title = $"{actorName} grubuna katildi",
                Message = $"\"{group.Name}\" topluluguna yeni bir uye eklendi.",
                Type = NotificationType.Social,
                TargetPath = "/communities"
            };
            dbContext.Notifications.Add(createdNotification);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (createdNotification is not null)
        {
            await notificationPublisher.PublishAsync(createdNotification, cancellationToken);
        }

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

    [HttpPost("{id:guid}/members/{targetUserId:guid}/promote")]
    public async Task<IActionResult> PromoteMember(
        Guid id,
        Guid targetUserId,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var group = await dbContext.Groups
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (group is null)
        {
            return NotFound();
        }

        var actorMembership = group.Members.FirstOrDefault(x => x.UserId == currentUserId.Value);
        if (actorMembership?.Role != GroupMemberRole.Owner)
        {
            return Forbid();
        }

        var targetMembership = group.Members.FirstOrDefault(x => x.UserId == targetUserId);
        if (targetMembership is null)
        {
            return NotFound();
        }

        if (targetMembership.UserId == currentUserId.Value)
        {
            return BadRequest(new { message = "Kendi rolunuzu degistiremezsiniz." });
        }

        if (targetMembership.Role != GroupMemberRole.Member)
        {
            return BadRequest(new { message = "Sadece normal uyeler moderator yapilabilir." });
        }

        targetMembership.Role = GroupMemberRole.Moderator;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/members/{targetUserId:guid}/demote")]
    public async Task<IActionResult> DemoteMember(
        Guid id,
        Guid targetUserId,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var group = await dbContext.Groups
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (group is null)
        {
            return NotFound();
        }

        var actorMembership = group.Members.FirstOrDefault(x => x.UserId == currentUserId.Value);
        if (actorMembership?.Role != GroupMemberRole.Owner)
        {
            return Forbid();
        }

        var targetMembership = group.Members.FirstOrDefault(x => x.UserId == targetUserId);
        if (targetMembership is null)
        {
            return NotFound();
        }

        if (targetMembership.UserId == currentUserId.Value)
        {
            return BadRequest(new { message = "Kendi rolunuzu degistiremezsiniz." });
        }

        if (targetMembership.Role != GroupMemberRole.Moderator)
        {
            return BadRequest(new { message = "Sadece moderatorler uye rolune indirilebilir." });
        }

        targetMembership.Role = GroupMemberRole.Member;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/members/{targetUserId:guid}")]
    public async Task<IActionResult> RemoveMember(
        Guid id,
        Guid targetUserId,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var group = await dbContext.Groups
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (group is null)
        {
            return NotFound();
        }

        var actorMembership = group.Members.FirstOrDefault(x => x.UserId == currentUserId.Value);
        if (actorMembership is null || actorMembership.Role == GroupMemberRole.Member)
        {
            return Forbid();
        }

        var targetMembership = group.Members.FirstOrDefault(x => x.UserId == targetUserId);
        if (targetMembership is null)
        {
            return NotFound();
        }

        if (targetMembership.UserId == currentUserId.Value)
        {
            return BadRequest(new { message = "Kendinizi topluluktan cikarmazsiniz." });
        }

        if (targetMembership.Role == GroupMemberRole.Owner)
        {
            return BadRequest(new { message = "Topluluk sahibi topluluktan cikarilamaz." });
        }

        if (actorMembership.Role == GroupMemberRole.Moderator && targetMembership.Role != GroupMemberRole.Member)
        {
            return Forbid();
        }

        dbContext.GroupMembers.Remove(targetMembership);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<Group> QueryGroups()
    {
        return dbContext.Groups
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => x.IsActive)
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
            .Where(x => !x.IsDeleted && x.GroupId.HasValue && x.Group != null && x.Group.IsActive)
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

    private static string BuildShortDescription(string? shortDescription, string description)
    {
        var preferredValue = string.IsNullOrWhiteSpace(shortDescription)
            ? description
            : shortDescription;

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

    private static string SerializeRules(IReadOnlyCollection<string>? rules)
    {
        var normalizedRules = (rules ?? [])
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();

        return JsonSerializer.Serialize(normalizedRules);
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

    private static GroupMemberRole? ResolveCurrentUserRole(Group group, Guid? currentUserId)
    {
        if (!currentUserId.HasValue)
        {
            return null;
        }

        return group.Members
            .FirstOrDefault(x => x.UserId == currentUserId.Value)
            ?.Role;
    }
}

public sealed record UploadImageResponse(string Url);
