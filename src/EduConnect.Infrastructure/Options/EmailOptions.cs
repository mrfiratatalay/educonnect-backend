namespace EduConnect.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string SenderName { get; init; } = "EduConnect";

    public string SenderEmail { get; init; } = "no-reply@educonnect.local";

    public string SmtpHost { get; init; } = string.Empty;

    public int SmtpPort { get; init; } = 25;

    public bool EnableSsl { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }
}
