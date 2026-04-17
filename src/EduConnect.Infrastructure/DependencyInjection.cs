using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Data;
using EduConnect.Infrastructure.Options;
using EduConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.Configure<AvatarUploadOptions>(configuration.GetSection(AvatarUploadOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddHttpContextAccessor();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IUserAvatarStorageService, UserAvatarStorageService>();
        services.AddSingleton<IPostMediaStorageService, PostMediaStorageService>();

        services.AddSingleton<IGeminiApiService, GeminiApiService>();
        services.AddScoped<IChatbotService, ChatbotService>();
        services.AddScoped<IVisualSearchService, VisualSearchService>();
        services.AddScoped<IMessagingService, MessagingService>();

        return services;
    }
}
