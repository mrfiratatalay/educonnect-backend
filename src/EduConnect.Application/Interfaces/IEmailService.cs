namespace EduConnect.Application.Interfaces;

public interface IEmailService
{
    Task SendForgotPasswordEmailAsync(string email, CancellationToken cancellationToken = default);
}
