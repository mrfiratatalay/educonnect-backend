using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace EduConnect.Infrastructure.Services;

public sealed class EmailService(
    IOptions<EmailOptions> options,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendEmailVerificationCodeAsync(
        string email,
        string fullName,
        string verificationCode,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var subject = "EduConnect e-posta dogrulama kodunuz";
        var body = $"""
                    Merhaba {fullName},

                    EduConnect hesabinizi dogrulamak icin kullanacaginiz kod:

                    {verificationCode}

                    Bu kod {expiresAtUtc:dd.MM.yyyy HH:mm} UTC zamanina kadar gecerlidir.

                    Eger bu kaydi siz olusturmadiysaniz bu e-postayi dikkate almayin.
                    """;

        await SendEmailAsync(email, subject, body, cancellationToken);
    }

    public Task SendForgotPasswordEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Forgot password mail requested for {Email}. Stub email service executed.", email);
        return Task.CompletedTask;
    }

    private async Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            throw new InvalidOperationException("E-posta servisi henuz yapilandirilmamis.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.SenderEmail, _options.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(recipientEmail);

        using var smtpClient = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            smtpClient.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        using var registration = cancellationToken.Register(smtpClient.SendAsyncCancel);
        await smtpClient.SendMailAsync(message, cancellationToken);
    }
}
