using EduConnect.Application.Contracts.Auth;

namespace EduConnect.Application.Interfaces;

public interface IAuthService
{
    Task<EmailVerificationChallengeResult> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedUserResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedUserResult> RefreshAsync(
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        Guid? userId,
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationChallengeResult> ResendEmailVerificationAsync(
        ResendEmailVerificationRequest request,
        CancellationToken cancellationToken = default);
}
