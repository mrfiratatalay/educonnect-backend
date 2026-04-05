using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using EduConnect.Api.Hubs;
using EduConnect.Api.Middleware;
using EduConnect.Application;
using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure;
using EduConnect.Infrastructure.Data;
using EduConnect.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("gemini", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 5;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EduConnect API",
        Version = "v1",
        Description = "EduConnect backend API for social, academic and marketplace modules."
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Bearer token kullanin."
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme);
});

var app = builder.Build();
var avatarUploadOptions = app.Services.GetRequiredService<IOptions<AvatarUploadOptions>>().Value;
var uploadsRootPath = Path.Combine(app.Environment.ContentRootPath, avatarUploadOptions.RootFolderName);

Directory.CreateDirectory(uploadsRootPath);

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRootPath),
    RequestPath = avatarUploadOptions.RequestPath,
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
    }
});
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    timestampUtc = DateTime.UtcNow
}));

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/test/chat", async (IGeminiApiService gemini, string? message) =>
    {
        var msg = message ?? "Merhaba, sen kimsin?";
        var reply = await gemini.GenerateTextAsync(msg);
        return Results.Ok(new { question = msg, reply });
    }).AllowAnonymous();

    app.MapPost("/api/test/vision", async (IGeminiApiService gemini, IFormFile image) =>
    {
        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);
        var prompt = "Bu gorseli analiz et ve JSON formatinda yanit ver: " +
                     "{\"productName\": \"...\", \"category\": \"...\", \"keywords\": [...], \"description\": \"...\"}";
        var result = await gemini.AnalyzeImageAsync(ms.ToArray(), image.ContentType!, prompt);
        return Results.Ok(new { analysis = result });
    }).AllowAnonymous().DisableAntiforgery();
}

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

await InitializeDatabaseAsync(app);

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");

    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHashService = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();

        await dbContext.Database.MigrateAsync();
        await DbSeeder.SeedAsync(dbContext, passwordHashService);
    }
    catch (Exception exception)
    {
        logger.LogWarning(exception, "Database migration/seed step skipped because the database is not currently reachable.");
    }
}
