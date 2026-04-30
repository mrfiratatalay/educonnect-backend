using EduConnect.Api.Mappings;
using EduConnect.Application.Contracts.Auth;
using EduConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduConnect.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IAuthService authService,
    ICurrentUserService currentUserService) : ControllerBase
{
    private const string RefreshTokenCookieName = "educonnect.refreshToken";

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<EmailVerificationChallengeResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, ClientIpAddress, cancellationToken);
        return Ok(result.ToChallengeResponse("Dogrulama kodu universite e-posta adresinize gonderildi."));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthSessionResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, ClientIpAddress, cancellationToken);
        AppendRefreshTokenCookie(result);
        return Ok(result.ToResponse());
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthSessionResponse>> Refresh(CancellationToken cancellationToken)
    {
        try
        {
            var result = await authService.RefreshAsync(RefreshTokenCookie, ClientIpAddress, cancellationToken);
            AppendRefreshTokenCookie(result);
            return Ok(result.ToResponse());
        }
        catch (UnauthorizedAccessException)
        {
            DeleteRefreshTokenCookie();
            throw;
        }
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(currentUserService.UserId, RefreshTokenCookie, ClientIpAddress, cancellationToken);
        DeleteRefreshTokenCookie();
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request, cancellationToken);

        return Ok(new
        {
            message = "Eğer kullanıcı mevcutsa şifre sıfırlama işlemi başlatılmıştır."
        });
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        await authService.VerifyEmailAsync(request, cancellationToken);

        return Ok(new
        {
            message = "Kurumsal e-posta adresiniz dogrulandi. Artik giris yapabilirsiniz."
        });
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<ActionResult<EmailVerificationChallengeResponse>> ResendVerification(
        [FromBody] ResendEmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.ResendEmailVerificationAsync(request, cancellationToken);

        return Ok(result.ToChallengeResponse("Yeni dogrulama kodu universite e-posta adresinize gonderildi."));
    }

    private string? RefreshTokenCookie =>
        Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken)
            ? refreshToken
            : null;

    private string? ClientIpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();

    private void AppendRefreshTokenCookie(AuthenticatedUserResult result)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            result.RefreshToken,
            CreateRefreshTokenCookieOptions(result.RefreshTokenExpiresAtUtc));
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            CreateRefreshTokenCookieOptions(DateTime.UtcNow.AddDays(-1)));
    }

    private CookieOptions CreateRefreshTokenCookieOptions(DateTime expiresAtUtc)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = new DateTimeOffset(expiresAtUtc),
            Path = "/"
        };
    }
}
