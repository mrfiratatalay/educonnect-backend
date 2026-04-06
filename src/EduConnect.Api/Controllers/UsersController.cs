using EduConnect.Api.Mappings;
using EduConnect.Application.Contracts.Users;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class UsersController(AppDbContext dbContext, ICurrentUserService currentUserService) : ControllerBase
{
    private const int DefaultFollowSuggestionLimit = 3;

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMe(CancellationToken cancellationToken)
    {
        var user = await QueryUsers()
            .FirstOrDefaultAsync(x => x.Id == currentUserService.UserId, cancellationToken);

        return user is null ? NotFound() : Ok(user.ToResponse());
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateMe(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        if (request.UniversityId.HasValue &&
            !await dbContext.Universities.AnyAsync(x => x.Id == request.UniversityId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Seçilen üniversite bulunamadı." });
        }

        var user = await dbContext.Users
            .Include(x => x.StudentProfile)
            .Include(x => x.University)
            .FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        user.FullName = request.FullName.Trim();
        user.UniversityId = request.UniversityId;

        if (user.StudentProfile is null)
        {
            user.StudentProfile = new StudentProfile
            {
                UserId = user.Id
            };
        }

        user.StudentProfile.Department = request.Department.Trim();
        user.StudentProfile.Year = request.Year;
        user.StudentProfile.Bio = request.Bio?.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        user = await QueryUsers().FirstAsync(x => x.Id == userId.Value, cancellationToken);
        return Ok(user.ToResponse());
    }

    [HttpPost("me/avatar")]
    public async Task<ActionResult<UserProfileResponse>> UploadAvatar(
        [FromBody] UploadAvatarRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await dbContext.Users
            .Include(x => x.StudentProfile)
            .FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (user.StudentProfile is null)
        {
            user.StudentProfile = new StudentProfile
            {
                UserId = user.Id,
                Department = string.Empty,
                Year = 1
            };
        }

        user.StudentProfile.AvatarUrl = request.AvatarUrl.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        user = await QueryUsers().FirstAsync(x => x.Id == userId.Value, cancellationToken);
        return Ok(user.ToResponse());
    }

    [HttpPost("me/avatar/upload")]
    public async Task<ActionResult<UserProfileResponse>> UploadAvatarFile(
        [FromForm(Name = "file")] IFormFile file,
        [FromServices] IUserAvatarStorageService userAvatarStorageService,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        if (file is null)
        {
            return BadRequest(new { message = "Yuklenecek dosya bulunamadi." });
        }

        var user = await dbContext.Users
            .Include(x => x.StudentProfile)
            .Include(x => x.University)
            .FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (user.StudentProfile is null)
        {
            user.StudentProfile = new StudentProfile
            {
                UserId = user.Id,
                Department = string.Empty,
                Year = 1
            };
        }

        await using var fileStream = file.OpenReadStream();
        user.StudentProfile.AvatarUrl = await userAvatarStorageService.SaveAvatarAsync(
            user.Id,
            fileStream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        user = await QueryUsers().FirstAsync(x => x.Id == userId.Value, cancellationToken);
        return Ok(user.ToResponse());
    }

    [HttpPost("me/cover/upload")]
    public async Task<ActionResult<UserProfileResponse>> UploadCoverFile(
        [FromForm(Name = "file")] IFormFile file,
        [FromServices] IUserAvatarStorageService userAvatarStorageService,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        if (file is null)
        {
            return BadRequest(new { message = "Yuklenecek dosya bulunamadi." });
        }

        var user = await dbContext.Users
            .Include(x => x.StudentProfile)
            .Include(x => x.University)
            .FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (user.StudentProfile is null)
        {
            user.StudentProfile = new StudentProfile
            {
                UserId = user.Id,
                Department = string.Empty,
                Year = 1
            };
        }

        await using var fileStream = file.OpenReadStream();
        user.StudentProfile.CoverImageUrl = await userAvatarStorageService.SaveCoverAsync(
            user.Id,
            fileStream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        user = await QueryUsers().FirstAsync(x => x.Id == userId.Value, cancellationToken);
        return Ok(user.ToResponse());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PublicUserProfileResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await QueryUsers()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = currentUserService.UserId;
        var isFollowedByCurrentUser = currentUserId.HasValue &&
                                      user.FollowerRelationships.Any(x => x.FollowerUserId == currentUserId.Value);

        return Ok(user.ToPublicResponse(isFollowedByCurrentUser));
    }

    [HttpGet("follow-suggestions")]
    public async Task<ActionResult<IReadOnlyCollection<FollowSuggestionResponse>>> GetFollowSuggestions(
        [FromQuery] int limit = DefaultFollowSuggestionLimit,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        limit = Math.Clamp(limit, 1, 12);

        var currentUserContext = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId.Value)
            .Select(x => new CurrentUserFollowContext(
                x.UniversityId,
                x.StudentProfile != null ? x.StudentProfile.Department : null))
            .FirstOrDefaultAsync(cancellationToken);

        if (currentUserContext is null)
        {
            return NotFound();
        }

        var followedUserIds = await dbContext.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowerUserId == userId.Value)
            .Select(x => x.FollowedUserId)
            .ToArrayAsync(cancellationToken);

        var currentGroupIds = await dbContext.GroupMembers
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value)
            .Select(x => x.GroupId)
            .ToArrayAsync(cancellationToken);

        var activeSinceUtc = DateTime.UtcNow.AddDays(-14);

        var suggestionCandidates = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.IsActive && x.Id != userId.Value && !followedUserIds.Contains(x.Id))
            .Select(x => new FollowSuggestionCandidate(
                x.Id,
                x.FullName,
                x.StudentProfile != null ? x.StudentProfile.AvatarUrl : null,
                x.StudentProfile != null ? x.StudentProfile.Department : null,
                x.UniversityId,
                x.University != null ? x.University.Name : null,
                x.GroupMemberships.Count(membership => currentGroupIds.Contains(membership.GroupId)),
                x.Posts.Count(post => !post.IsDeleted && !post.GroupId.HasValue && post.CreatedAtUtc >= activeSinceUtc)))
            .ToListAsync(cancellationToken);

        var suggestions = suggestionCandidates
            .OrderByDescending(x => x.UniversityId == currentUserContext.UniversityId && currentUserContext.UniversityId.HasValue)
            .ThenByDescending(x => x.MutualGroupCount)
            .ThenByDescending(x => x.RecentPersonalPostCount)
            .ThenBy(x => x.FullName)
            .Take(limit)
            .ToArray();

        return Ok(suggestions.Select(x => new FollowSuggestionResponse
        {
            Id = x.Id,
            FullName = x.FullName,
            AvatarUrl = x.AvatarUrl,
            Department = x.Department,
            UniversityName = x.UniversityName,
            MutualGroupCount = x.MutualGroupCount,
            ReasonLabel = BuildFollowSuggestionReason(x, currentUserContext)
        }).ToArray());
    }

    [HttpGet("{id:guid}/followers")]
    public async Task<ActionResult<IReadOnlyCollection<UserConnectionResponse>>> GetFollowers(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var targetExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (!targetExists)
        {
            return NotFound();
        }

        var followedUserIds = await dbContext.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowerUserId == currentUserId.Value)
            .Select(x => x.FollowedUserId)
            .ToArrayAsync(cancellationToken);

        var followers = await dbContext.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowedUserId == id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.FollowerUser)
            .Select(x => new UserConnectionResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                AvatarUrl = x.StudentProfile != null ? x.StudentProfile.AvatarUrl : null,
                Department = x.StudentProfile != null ? x.StudentProfile.Department : null,
                UniversityName = x.University != null ? x.University.Name : null,
                IsFollowedByCurrentUser = followedUserIds.Contains(x.Id)
            })
            .ToArrayAsync(cancellationToken);

        return Ok(followers);
    }

    [HttpGet("{id:guid}/following")]
    public async Task<ActionResult<IReadOnlyCollection<UserConnectionResponse>>> GetFollowing(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var targetExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (!targetExists)
        {
            return NotFound();
        }

        var followedUserIds = await dbContext.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowerUserId == currentUserId.Value)
            .Select(x => x.FollowedUserId)
            .ToArrayAsync(cancellationToken);

        var following = await dbContext.UserFollows
            .AsNoTracking()
            .Where(x => x.FollowerUserId == id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.FollowedUser)
            .Select(x => new UserConnectionResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                AvatarUrl = x.StudentProfile != null ? x.StudentProfile.AvatarUrl : null,
                Department = x.StudentProfile != null ? x.StudentProfile.Department : null,
                UniversityName = x.University != null ? x.University.Name : null,
                IsFollowedByCurrentUser = followedUserIds.Contains(x.Id)
            })
            .ToArrayAsync(cancellationToken);

        return Ok(following);
    }

    [HttpPost("{id:guid}/follow")]
    public async Task<ActionResult<FollowStateResponse>> Follow(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        if (id == userId.Value)
        {
            return BadRequest(new { message = "Kullanici kendini takip edemez." });
        }

        var targetExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (!targetExists)
        {
            return NotFound();
        }

        var isAlreadyFollowing = await dbContext.UserFollows
            .AnyAsync(
                x => x.FollowerUserId == userId.Value && x.FollowedUserId == id,
                cancellationToken);

        if (!isAlreadyFollowing)
        {
            dbContext.UserFollows.Add(new UserFollow
            {
                FollowerUserId = userId.Value,
                FollowedUserId = id
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new FollowStateResponse
        {
            IsFollowing = true
        });
    }

    [HttpDelete("{id:guid}/follow")]
    public async Task<ActionResult<FollowStateResponse>> Unfollow(Guid id, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var relationship = await dbContext.UserFollows
            .FirstOrDefaultAsync(
                x => x.FollowerUserId == userId.Value && x.FollowedUserId == id,
                cancellationToken);

        if (relationship is not null)
        {
            dbContext.UserFollows.Remove(relationship);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new FollowStateResponse
        {
            IsFollowing = false
        });
    }

    private IQueryable<User> QueryUsers()
    {
        return dbContext.Users
            .AsNoTracking()
            .Include(x => x.StudentProfile)
            .Include(x => x.University)
            .Include(x => x.FollowerRelationships)
            .Include(x => x.FollowingRelationships);
    }

    private static string BuildFollowSuggestionReason(
        FollowSuggestionCandidate suggestion,
        CurrentUserFollowContext currentUser)
    {
        if (!string.IsNullOrWhiteSpace(currentUser.Department) &&
            !string.IsNullOrWhiteSpace(suggestion.Department) &&
            string.Equals(currentUser.Department, suggestion.Department, StringComparison.OrdinalIgnoreCase))
        {
            return $"{suggestion.Department} bolumunden";
        }

        if (suggestion.MutualGroupCount > 0)
        {
            return suggestion.MutualGroupCount == 1
                ? "1 ortak topluluk"
                : $"{suggestion.MutualGroupCount} ortak topluluk";
        }

        if (currentUser.UniversityId.HasValue &&
            suggestion.UniversityId == currentUser.UniversityId)
        {
            return "Ayni universitede";
        }

        if (suggestion.RecentPersonalPostCount > 0)
        {
            return "Bu hafta aktif";
        }

        return "Kesfetmeye deger";
    }

    private sealed record CurrentUserFollowContext(
        Guid? UniversityId,
        string? Department);

    private sealed record FollowSuggestionCandidate(
        Guid Id,
        string FullName,
        string? AvatarUrl,
        string? Department,
        Guid? UniversityId,
        string? UniversityName,
        int MutualGroupCount,
        int RecentPersonalPostCount);
}
