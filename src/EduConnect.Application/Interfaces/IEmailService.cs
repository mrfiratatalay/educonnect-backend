namespace EduConnect.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationCodeAsync(
        string email,
        string fullName,
        string verificationCode,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task SendForgotPasswordEmailAsync(string email, CancellationToken cancellationToken = default);
}
