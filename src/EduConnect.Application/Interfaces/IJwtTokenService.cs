using EduConnect.Domain.Entities;

namespace EduConnect.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);

    string GenerateRefreshToken();

    DateTime GetAccessTokenExpiryUtc();

    DateTime GetRefreshTokenExpiryUtc();
}
