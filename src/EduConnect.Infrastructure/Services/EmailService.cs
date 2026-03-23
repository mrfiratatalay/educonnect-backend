using EduConnect.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EduConnect.Infrastructure.Services;

public sealed class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendForgotPasswordEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Forgot password mail requested for {Email}. Stub email service executed.", email);
        return Task.CompletedTask;
    }
}
