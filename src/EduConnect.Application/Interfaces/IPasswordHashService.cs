using EduConnect.Domain.Entities;

namespace EduConnect.Application.Interfaces;

public interface IPasswordHashService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string hashedPassword, string providedPassword);
}
