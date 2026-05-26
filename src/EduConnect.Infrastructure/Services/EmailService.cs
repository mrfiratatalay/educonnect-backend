using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;

namespace EduConnect.Infrastructure.Services;

public sealed class EmailService(
    IOptions<EmailOptions> options,
    IHttpClientFactory httpClientFactory,
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
        if (!string.IsNullOrWhiteSpace(_options.BrevoApiKey))
        {
            await SendViaBrevoAsync(recipientEmail, subject, body, cancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            await SendViaSmtpAsync(recipientEmail, subject, body, cancellationToken);
            return;
        }

        throw new InvalidOperationException("E-posta servisi henuz yapilandirilmamis.");
    }

    private async Task SendViaBrevoAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var http = httpClientFactory.CreateClient();
        http.BaseAddress = new Uri("https://api.brevo.com/v3/");
        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        http.DefaultRequestHeaders.Remove("api-key");
        http.DefaultRequestHeaders.Add("api-key", _options.BrevoApiKey);

        var payload = new
        {
            sender = new { name = _options.SenderName, email = _options.SenderEmail },
            to = new[] { new { email = recipientEmail } },
            subject,
            textContent = body
        };

        using var response = await http.PostAsJsonAsync("smtp/email", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Brevo API mail send failed: {Status} {Body}", response.StatusCode, error);
            throw new InvalidOperationException($"Mail gonderilemedi (Brevo): {(int)response.StatusCode}");
        }

        logger.LogInformation("Verification email sent via Brevo to {Recipient}", recipientEmail);
    }

    private async Task SendViaSmtpAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
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
        logger.LogInformation("Verification email sent via SMTP to {Recipient}", recipientEmail);
    }
}
