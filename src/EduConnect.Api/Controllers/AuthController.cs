using EduConnect.Application.Contracts.Auth;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    AppDbContext dbContext,
    IPasswordHashService passwordHashService,
    IJwtTokenService jwtTokenService,
    IEmailService emailService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            return Conflict(new { message = "Bu email adresi zaten kayıtlı." });
        }

        if (request.UniversityId.HasValue &&
            !await dbContext.Universities.AnyAsync(x => x.Id == request.UniversityId.Value, cancellationToken))
        {
            return BadRequest(new { message = "Seçilen üniversite bulunamadı." });
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            UniversityId = request.UniversityId
        };

        user.PasswordHash = passwordHashService.HashPassword(user, request.Password);
        user.StudentProfile = new StudentProfile
        {
            Department = request.Department.Trim(),
            Year = request.Year
        };

        var refreshToken = CreateRefreshToken(user);

        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(refreshToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(CreateAuthResponse(user, refreshToken.Token));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail && x.IsActive, cancellationToken);

        if (user is null || !passwordHashService.VerifyPassword(user, user.PasswordHash, request.Password))
        {
            return Unauthorized(new { message = "Email veya şifre hatalı." });
        }

        var refreshToken = CreateRefreshToken(user);
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(CreateAuthResponse(user, refreshToken.Token));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var existingRefreshToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

        if (existingRefreshToken is null || !existingRefreshToken.IsActive || !existingRefreshToken.User.IsActive)
        {
            return Unauthorized(new { message = "Refresh token geçersiz veya süresi dolmuş." });
        }

        existingRefreshToken.RevokedAtUtc = DateTime.UtcNow;
        var replacementToken = CreateRefreshToken(existingRefreshToken.User);
        existingRefreshToken.ReplacedByToken = replacementToken.Token;

        dbContext.RefreshTokens.Add(replacementToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(CreateAuthResponse(existingRefreshToken.User, replacementToken.Token));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId;

        if (userId is null)
        {
            return Unauthorized();
        }

        var refreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken && x.UserId == userId.Value, cancellationToken);

        if (refreshToken is null)
        {
            return NotFound(new { message = "Refresh token bulunamadı." });
        }

        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var userExists = await dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (userExists)
        {
            await emailService.SendForgotPasswordEmailAsync(normalizedEmail, cancellationToken);
        }

        return Ok(new
        {
            message = "Eğer kullanıcı mevcutsa şifre sıfırlama işlemi başlatılmıştır."
        });
    }

    private Guid? UserId =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)
            ? userId
            : null;

    private RefreshToken CreateRefreshToken(User user)
    {
        return new RefreshToken
        {
            UserId = user.Id,
            Token = jwtTokenService.GenerateRefreshToken(),
            ExpiresAtUtc = jwtTokenService.GetRefreshTokenExpiryUtc(),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
    }

    private AuthResponse CreateAuthResponse(User user, string refreshToken)
    {
        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            AccessToken = jwtTokenService.GenerateAccessToken(user),
            RefreshToken = refreshToken,
            ExpiresAtUtc = jwtTokenService.GetAccessTokenExpiryUtc()
        };
    }
}
