using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduConnect.Api.Common;

/// <summary>
/// Avatar / cover / post media / group cover gibi gorsel upload endpoint'leri icin ortak validasyon.
/// Bos dosya, boyut tavani, MIME beyaz listesi kontrol eder.
/// </summary>
public static class FileUploadValidation
{
    public const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

    public static readonly HashSet<string> AllowedImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif",
    };

    /// <summary>
    /// Gorseli dogrular. Eger gecersizse hata mesajini doner, gecerliyse null doner.
    /// Caller `if (error != null) return BadRequest(new { message = error });` seklinde kullanmali.
    /// </summary>
    public static string? ValidateImage(IFormFile? image, long? maxBytes = null)
    {
        if (image is null || image.Length == 0)
        {
            return "Görsel boş veya yüklenmedi.";
        }

        var sizeLimit = maxBytes ?? MaxImageBytes;
        if (image.Length > sizeLimit)
        {
            var mb = sizeLimit / 1024 / 1024;
            return $"Görsel {mb}MB'dan büyük olamaz.";
        }

        if (!AllowedImageMimeTypes.Contains(image.ContentType ?? string.Empty))
        {
            return "Desteklenmeyen format. JPEG, PNG, WEBP veya GIF kullanın.";
        }

        return null;
    }
}
