using System.ComponentModel.DataAnnotations;
using EduConnect.Application.Contracts.Users;
using EduConnect.Domain.Entities;

namespace EduConnect.Application.Contracts.Auth;

public sealed class RegisterRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    public Guid UniversityId { get; init; }

    [Required, StringLength(150, MinimumLength = 2)]
    public string Department { get; init; } = string.Empty;

    [Range(1, 8)]
    public int Year { get; init; } = 1;
}

public sealed class EmailVerificationChallengeResponse
{
    public string Email { get; init; } = string.Empty;

    public DateTime VerificationExpiresAtUtc { get; init; }

    public DateTime CanResendAtUtc { get; init; }

    public string Message { get; init; } = string.Empty;
}

public sealed class EmailVerificationChallengeResult
{
    public string Email { get; init; } = string.Empty;

    public DateTime VerificationExpiresAtUtc { get; init; }

    public DateTime CanResendAtUtc { get; init; }
}

public sealed class VerifyEmailRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; init; } = string.Empty;
}

public sealed class ResendEmailVerificationRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;
}

public sealed class AuthSessionResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }

    public UserProfileResponse User { get; init; } = new();
}

public sealed class AuthenticatedUserResult
{
    public User User { get; init; } = null!;

    public string AccessToken { get; init; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; init; }

    public string RefreshToken { get; init; } = string.Empty;

    public DateTime RefreshTokenExpiresAtUtc { get; init; }
}
