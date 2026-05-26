using EduConnect.Application.Contracts.Auth;
using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Infrastructure.Data;
using EduConnect.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EduConnect.Infrastructure.Services;

public sealed class AuthService(
    AppDbContext dbContext,
    IPasswordHashService passwordHashService,
    IJwtTokenService jwtTokenService,
    IEmailService emailService) : IAuthService
{
    private static readonly TimeSpan EmailVerificationExpiry = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan EmailVerificationResendCooldown = TimeSpan.FromSeconds(60);

    public async Task<EmailVerificationChallengeResult> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateRegisterRequest(request);

        var normalizedEmail = NormalizeEmail(request.Email);
        var university = await GetUniversityForRegistrationAsync(request.UniversityId, cancellationToken);
        EnsureEmailMatchesUniversityDomain(normalizedEmail, university.Domain);

        // Iki paralel register istegi ayni email ile gelirse: ilkinin SaveChangesAsync'i basarili olur,
        // ikincisi unique constraint violation alir. Catch + 1 kez retry ile ikincide "existing user" yoluna girer.
        const int maxAttempts = 2;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await ExecuteRegisterAsync(request, normalizedEmail, cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.IsUniqueViolation() && attempt < maxAttempts)
            {
                // Paralel istek bu email'i once kaydetti. State'i temizleyip tekrar dene; bu kez existing-user yolu calisacak.
                dbContext.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException("Kayit yapilamadi, lutfen tekrar deneyin.");
    }

    private async Task<EmailVerificationChallengeResult> ExecuteRegisterAsync(
        RegisterRequest request,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var existingUser = await dbContext.Users
            .Include(item => item.StudentProfile)
            .FirstOrDefaultAsync(item => item.Email == normalizedEmail, cancellationToken);

        var verificationCode = GenerateVerificationCode();
        User user;

        if (existingUser is not null)
        {
            if (existingUser.EmailVerifiedAtUtc.HasValue)
            {
                throw new InvalidOperationException("Bu email adresi zaten kayitli.");
            }

            user = existingUser;
            user.FullName = request.FullName.Trim();
            user.UniversityId = request.UniversityId;
            user.PasswordHash = passwordHashService.HashPassword(user, request.Password);

            if (user.StudentProfile is null)
            {
                user.StudentProfile = new StudentProfile
                {
                    UserId = user.Id
                };
            }

            user.StudentProfile.Department = request.Department.Trim();
            user.StudentProfile.Year = request.Year;
        }
        else
        {
            user = new User
            {
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                UniversityId = request.UniversityId,
                StudentProfile = new StudentProfile
                {
                    Department = request.Department.Trim(),
                    Year = request.Year
                }
            };

            user.PasswordHash = passwordHashService.HashPassword(user, request.Password);
            dbContext.Users.Add(user);
        }

        var challenge = SetEmailVerificationChallenge(user, verificationCode);
        await dbContext.SaveChangesAsync(cancellationToken);

        await emailService.SendEmailVerificationCodeAsync(
            user.Email,
            user.FullName,
            verificationCode,
            challenge.VerificationExpiresAtUtc,
            cancellationToken);

        return challenge;
    }

    public async Task<AuthenticatedUserResult> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await QueryUsers()
            .FirstOrDefaultAsync(
                item => item.Email == normalizedEmail && item.IsActive,
                cancellationToken);

        if (user is null || !passwordHashService.VerifyPassword(user, user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Email veya sifre hatali.");
        }

        if (!user.EmailVerifiedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Kurumsal e-posta dogrulamasi tamamlanmadi.");
        }

        var refreshToken = CreateRefreshToken(user.Id, ipAddress);
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateAuthenticatedUserResult(user, refreshToken);
    }

    public async Task<AuthenticatedUserResult> RefreshAsync(
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("Oturum yenilenemedi.");
        }

        var nowUtc = DateTime.UtcNow;

        // ATOMIK CLAIM: Token'i hala aktifse revoke et. Paralel iki refresh isteginde
        // sadece birinin ExecuteUpdateAsync'i 1 satir guncelleyebilir; ikincisi 0 doner.
        // Bu sayede TOCTOU race condition kapaniyor.
        var revokedRows = await dbContext.RefreshTokens
            .Where(rt =>
                rt.Token == refreshToken
                && rt.RevokedAtUtc == null
                && rt.ExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(
                s => s.SetProperty(rt => rt.RevokedAtUtc, nowUtc),
                cancellationToken);

        if (revokedRows == 0)
        {
            throw new UnauthorizedAccessException("Oturum yenilenemedi.");
        }

        // Token'i claim ettik, simdi user'i guvenle yukleyip aktif olup olmadigini dogrula.
        var existingToken = await dbContext.RefreshTokens
            .AsNoTracking()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (existingToken is null || !existingToken.User.IsActive)
        {
            throw new UnauthorizedAccessException("Oturum yenilenemedi.");
        }

        // Yeni token uret + yaz.
        var replacementToken = CreateRefreshToken(existingToken.UserId, ipAddress);
        dbContext.RefreshTokens.Add(replacementToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Eski token'in ReplacedByToken alanini guncelle (audit trail).
        await dbContext.RefreshTokens
            .Where(rt => rt.Token == refreshToken)
            .ExecuteUpdateAsync(
                s => s.SetProperty(rt => rt.ReplacedByToken, replacementToken.Token),
                cancellationToken);

        return await CreateAuthenticatedUserResultAsync(
            existingToken.UserId,
            replacementToken,
            cancellationToken);
    }

    public async Task LogoutAsync(
        Guid? userId,
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var existingRefreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(item => item.Token == refreshToken, cancellationToken);

        if (existingRefreshToken is null || existingRefreshToken.RevokedAtUtc is not null)
        {
            return;
        }

        if (userId.HasValue && existingRefreshToken.UserId != userId.Value)
        {
            return;
        }

        existingRefreshToken.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var userExists = await dbContext.Users
            .AnyAsync(item => item.Email == normalizedEmail, cancellationToken);

        if (userExists)
        {
            await emailService.SendForgotPasswordEmailAsync(normalizedEmail, cancellationToken);
        }
    }

    public async Task VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await dbContext.Users
            .FirstOrDefaultAsync(item => item.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("Dogrulama bekleyen hesap bulunamadi.");
        }

        if (user.EmailVerifiedAtUtc.HasValue)
        {
            return;
        }

        if (user.EmailVerificationExpiresAtUtc is null ||
            user.EmailVerificationSentAtUtc is null ||
            string.IsNullOrWhiteSpace(user.EmailVerificationCodeHash))
        {
            throw new InvalidOperationException("Aktif bir dogrulama kodu bulunamadi.");
        }

        if (user.EmailVerificationExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Dogrulama kodunun suresi doldu.");
        }

        user.EmailVerificationAttemptCount += 1;
        if (user.EmailVerificationAttemptCount > 5)
        {
            throw new InvalidOperationException("Cok fazla hatali deneme yaptiniz. Yeni bir kod isteyin.");
        }

        var normalizedCode = NormalizeVerificationCode(request.Code);
        var codeHash = HashVerificationCode(normalizedCode);

        if (!string.Equals(user.EmailVerificationCodeHash, codeHash, StringComparison.Ordinal))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Dogrulama kodu hatali.");
        }

        user.EmailVerifiedAtUtc = DateTime.UtcNow;
        user.EmailVerificationCodeHash = null;
        user.EmailVerificationExpiresAtUtc = null;
        user.EmailVerificationSentAtUtc = null;
        user.EmailVerificationAttemptCount = 0;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmailVerificationChallengeResult> ResendEmailVerificationAsync(
        ResendEmailVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await dbContext.Users
            .FirstOrDefaultAsync(item => item.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("Dogrulama bekleyen hesap bulunamadi.");
        }

        if (user.EmailVerifiedAtUtc.HasValue)
        {
            throw new InvalidOperationException("Bu hesap zaten dogrulandi.");
        }

        if (user.EmailVerificationSentAtUtc.HasValue)
        {
            var nextAllowedResendAtUtc = user.EmailVerificationSentAtUtc.Value.Add(EmailVerificationResendCooldown);
            if (nextAllowedResendAtUtc > DateTime.UtcNow)
            {
                var remainingSeconds = (int)Math.Ceiling((nextAllowedResendAtUtc - DateTime.UtcNow).TotalSeconds);
                throw new InvalidOperationException($"Yeni kod istemek icin {remainingSeconds} saniye bekleyin.");
            }
        }

        var verificationCode = GenerateVerificationCode();
        var challenge = SetEmailVerificationChallenge(user, verificationCode);
        await dbContext.SaveChangesAsync(cancellationToken);

        await emailService.SendEmailVerificationCodeAsync(
            user.Email,
            user.FullName,
            verificationCode,
            challenge.VerificationExpiresAtUtc,
            cancellationToken);

        return challenge;
    }

    private IQueryable<User> QueryUsers()
    {
        return dbContext.Users
            .AsNoTracking()
            .Include(item => item.StudentProfile)
            .Include(item => item.University);
    }

    private async Task<AuthenticatedUserResult> CreateAuthenticatedUserResultAsync(
        Guid userId,
        RefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        var user = await QueryUsers()
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Kullanici bulunamadi.");

        return CreateAuthenticatedUserResult(user, refreshToken);
    }

    private AuthenticatedUserResult CreateAuthenticatedUserResult(User user, RefreshToken refreshToken)
    {
        return new AuthenticatedUserResult
        {
            User = user,
            AccessToken = jwtTokenService.GenerateAccessToken(user),
            AccessTokenExpiresAtUtc = jwtTokenService.GetAccessTokenExpiryUtc(),
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc
        };
    }

    private RefreshToken CreateRefreshToken(Guid userId, string? ipAddress)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = jwtTokenService.GenerateRefreshToken(),
            ExpiresAtUtc = jwtTokenService.GetRefreshTokenExpiryUtc(),
            CreatedByIp = ipAddress
        };
    }

    private async Task<University> GetUniversityForRegistrationAsync(Guid universityId, CancellationToken cancellationToken)
    {
        var university = await dbContext.Universities
            .FirstOrDefaultAsync(item => item.Id == universityId, cancellationToken);

        if (university is null)
        {
            throw new InvalidOperationException("Secilen universite bulunamadi.");
        }

        return university;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string NormalizeVerificationCode(string code) => code.Trim();

    private static string GenerateVerificationCode()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    private static string HashVerificationCode(string code)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    }

    private static void EnsureEmailMatchesUniversityDomain(string normalizedEmail, string universityDomain)
    {
        var emailParts = normalizedEmail.Split('@', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (emailParts.Length != 2)
        {
            throw new InvalidOperationException("Gecerli bir universite e-posta adresi girin.");
        }

        var normalizedUniversityDomain = universityDomain.Trim().ToLowerInvariant();
        var emailDomain = emailParts[1].Trim().ToLowerInvariant();

        if (emailDomain == normalizedUniversityDomain)
        {
            return;
        }

        if (emailDomain.EndsWith($".{normalizedUniversityDomain}", StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException("Kayit icin secilen universiteye ait kurumsal e-posta adresi kullanilmalidir.");
    }

    private static EmailVerificationChallengeResult SetEmailVerificationChallenge(User user, string verificationCode)
    {
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.Add(EmailVerificationExpiry);

        user.EmailVerificationCodeHash = HashVerificationCode(verificationCode);
        user.EmailVerificationExpiresAtUtc = expiresAtUtc;
        user.EmailVerificationSentAtUtc = now;
        user.EmailVerificationAttemptCount = 0;

        return new EmailVerificationChallengeResult
        {
            Email = user.Email,
            VerificationExpiresAtUtc = expiresAtUtc,
            CanResendAtUtc = now.Add(EmailVerificationResendCooldown)
        };
    }

    private static void ValidateRegisterRequest(RegisterRequest request)
    {
        if (request.UniversityId == Guid.Empty)
        {
            throw new InvalidOperationException("Kayit icin universite secmelisiniz.");
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new InvalidOperationException("Ad soyad zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.Department))
        {
            throw new InvalidOperationException("Bolum zorunludur.");
        }
    }
}
