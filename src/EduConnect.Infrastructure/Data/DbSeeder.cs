using EduConnect.Application.Interfaces;
using EduConnect.Domain.Entities;
using EduConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, IPasswordHashService passwordHashService, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Universities.AnyAsync(cancellationToken))
        {
            dbContext.Universities.AddRange(
                new University
                {
                    Name = "Recep Tayyip Erdoğan Üniversitesi",
                    City = "Rize",
                    Domain = "erdogan.edu.tr"
                },
                new University
                {
                    Name = "Karadeniz Teknik Üniversitesi",
                    City = "Trabzon",
                    Domain = "ktu.edu.tr"
                });
        }

        if (!await dbContext.Categories.AnyAsync(cancellationToken))
        {
            dbContext.Categories.AddRange(
                new Category { Name = "Ders Kitapları" },
                new Category { Name = "Elektronik" },
                new Category { Name = "Kırtasiye" },
                new Category { Name = "Etkinlik Biletleri" });
        }

        if (!await dbContext.Users.AnyAsync(x => x.Role == UserRole.Admin, cancellationToken))
        {
            var admin = new User
            {
                FullName = "EduConnect Admin",
                Email = "admin@educonnect.local",
                Role = UserRole.Admin,
                TwoFactorEnabled = false
            };

            admin.PasswordHash = passwordHashService.HashPassword(admin, "Admin123!");

            dbContext.Users.Add(admin);
            dbContext.StudentProfiles.Add(new StudentProfile
            {
                User = admin,
                Department = "Computer Engineering",
                Year = 4,
                Bio = "Default administrator account."
            });
        }

        if (!await dbContext.Discounts.AnyAsync(cancellationToken))
        {
            dbContext.Discounts.AddRange(
                new Discount
                {
                    BusinessName = "Kampus Cafe",
                    Title = "Kahvede %20 indirim",
                    Description = "Öğrenci kimliği ile tüm sıcak içeceklerde geçerli.",
                    DiscountRate = 20,
                    DiscountCode = "KAMPUS20",
                    ValidUntilUtc = DateTime.UtcNow.AddMonths(3)
                },
                new Discount
                {
                    BusinessName = "BookHub",
                    Title = "Ders kitaplarında %15 indirim",
                    Description = "Seçili teknik kitaplarda geçerli kampanya.",
                    DiscountRate = 15,
                    DiscountCode = "BOOK15",
                    ValidUntilUtc = DateTime.UtcNow.AddMonths(2)
                });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
