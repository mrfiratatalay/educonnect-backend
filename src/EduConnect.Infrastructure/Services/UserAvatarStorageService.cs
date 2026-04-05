using System.Collections.Concurrent;
using EduConnect.Application.Interfaces;
using EduConnect.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduConnect.Infrastructure.Services;

public sealed class UserAvatarStorageService(
    IWebHostEnvironment environment,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AvatarUploadOptions> options,
    ILogger<UserAvatarStorageService> logger) : IUserAvatarStorageService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> UserLocks = new();
    private readonly AvatarUploadOptions uploadOptions = options.Value;

    public Task<string> SaveAvatarAsync(
        Guid userId,
        Stream fileStream,
        string? fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        return SaveAsync(
            userId,
            uploadOptions.AvatarsFolderName,
            fileStream,
            fileName,
            contentType,
            cancellationToken);
    }

    public Task<string> SaveCoverAsync(
        Guid userId,
        Stream fileStream,
        string? fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        return SaveAsync(
            userId,
            uploadOptions.CoversFolderName,
            fileStream,
            fileName,
            contentType,
            cancellationToken);
    }

    private async Task<string> SaveAsync(
        Guid userId,
        string mediaFolderName,
        Stream fileStream,
        string? fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        var bytes = await ReadValidatedFileAsync(fileStream, cancellationToken);
        var fileType = DetectFileType(bytes)
            ?? throw new InvalidOperationException("Yuklenen dosya desteklenen bir gorsel formati degil.");

        ValidateClientMetadata(fileName, contentType, fileType);

        var userLock = UserLocks.GetOrAdd(userId, static _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync(cancellationToken);

        try
        {
            var userFolderPath = Path.Combine(
                environment.ContentRootPath,
                uploadOptions.RootFolderName,
                mediaFolderName,
                userId.ToString("N"));

            Directory.CreateDirectory(userFolderPath);

            var storedFileName = $"{Guid.NewGuid():N}{fileType.Extension}";
            var absoluteFilePath = Path.Combine(userFolderPath, storedFileName);

            await File.WriteAllBytesAsync(absoluteFilePath, bytes, cancellationToken);
            DeleteStaleFiles(userFolderPath, absoluteFilePath);

            return BuildPublicUrl(userId, mediaFolderName, storedFileName);
        }
        finally
        {
            userLock.Release();
        }
    }

    private async Task<byte[]> ReadValidatedFileAsync(Stream fileStream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        if (!fileStream.CanRead)
        {
            throw new InvalidOperationException("Yuklenen dosya okunamiyor.");
        }

        var maxFileSizeBytes = uploadOptions.MaxFileSizeBytes;
        using var memoryStream = new MemoryStream(capacity: (int)Math.Min(maxFileSizeBytes, 1024 * 1024));
        var buffer = new byte[81920];
        long totalBytes = 0;

        while (true)
        {
            var bytesRead = await fileStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
            if (totalBytes > maxFileSizeBytes)
            {
                throw new InvalidOperationException(
                    $"Profil fotografi en fazla {maxFileSizeBytes / (1024 * 1024)} MB olabilir.");
            }

            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        if (totalBytes == 0)
        {
            throw new InvalidOperationException("Bos dosya yuklenemez.");
        }

        return memoryStream.ToArray();
    }

    private void ValidateClientMetadata(string? fileName, string? contentType, FileTypeInfo detectedFileType)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var providedExtension = Path.GetExtension(fileName);
            if (!string.IsNullOrWhiteSpace(providedExtension) &&
                !uploadOptions.AllowedExtensions.Contains(providedExtension, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Sadece JPG, PNG veya GIF profil fotografi yukleyebilirsiniz.");
            }
        }

        if (!string.IsNullOrWhiteSpace(contentType) &&
            !string.Equals(contentType, detectedFileType.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Yuklenen dosyanin icerik tipi gecerli degil.");
        }
    }

    private string BuildPublicUrl(Guid userId, string mediaFolderName, string storedFileName)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Avatar dosyasi icin genel erisim URL'si olusturulamadi.");

        var request = httpContext.Request;
        var requestPath = uploadOptions.RequestPath.StartsWith('/')
            ? uploadOptions.RequestPath
            : $"/{uploadOptions.RequestPath}";

        var relativePath = $"{requestPath.TrimEnd('/')}/{mediaFolderName}/{userId:N}/{storedFileName}";

        return $"{request.Scheme}://{request.Host}{request.PathBase}{relativePath}";
    }

    private void DeleteStaleFiles(string userFolderPath, string currentFilePath)
    {
        foreach (var filePath in Directory.EnumerateFiles(userFolderPath))
        {
            if (string.Equals(filePath, currentFilePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                File.Delete(filePath);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to delete stale avatar file at {FilePath}", filePath);
            }
        }
    }

    private static FileTypeInfo? DetectFileType(byte[] bytes)
    {
        ReadOnlySpan<byte> fileBytes = bytes;

        if (fileBytes.Length >= 3 &&
            fileBytes[0] == 0xFF &&
            fileBytes[1] == 0xD8 &&
            fileBytes[2] == 0xFF)
        {
            return new FileTypeInfo(".jpg", "image/jpeg");
        }

        if (fileBytes.Length >= 8 &&
            fileBytes[0] == 0x89 &&
            fileBytes[1] == 0x50 &&
            fileBytes[2] == 0x4E &&
            fileBytes[3] == 0x47 &&
            fileBytes[4] == 0x0D &&
            fileBytes[5] == 0x0A &&
            fileBytes[6] == 0x1A &&
            fileBytes[7] == 0x0A)
        {
            return new FileTypeInfo(".png", "image/png");
        }

        if (fileBytes.Length >= 6 &&
            fileBytes[0] == 0x47 &&
            fileBytes[1] == 0x49 &&
            fileBytes[2] == 0x46 &&
            fileBytes[3] == 0x38 &&
            (fileBytes[4] == 0x37 || fileBytes[4] == 0x39) &&
            fileBytes[5] == 0x61)
        {
            return new FileTypeInfo(".gif", "image/gif");
        }

        return null;
    }

    private sealed record FileTypeInfo(string Extension, string ContentType);
}
