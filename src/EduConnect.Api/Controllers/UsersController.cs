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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserProfileResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await QueryUsers()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return user is null ? NotFound() : Ok(user.ToResponse());
    }

    private IQueryable<User> QueryUsers()
    {
        return dbContext.Users
            .AsNoTracking()
            .Include(x => x.StudentProfile)
            .Include(x => x.University);
    }
}
