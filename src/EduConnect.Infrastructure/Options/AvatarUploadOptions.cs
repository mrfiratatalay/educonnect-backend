namespace EduConnect.Infrastructure.Options;

public sealed class AvatarUploadOptions
{
    public const string SectionName = "AvatarUpload";

    public string RootFolderName { get; init; } = "uploads";

    public string RequestPath { get; init; } = "/uploads";

    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;

    public string AvatarsFolderName { get; init; } = "avatars";

    public string CoversFolderName { get; init; } = "covers";

    public string PostsFolderName { get; init; } = "posts";

    public string[] AllowedExtensions { get; init; } = [".jpg", ".jpeg", ".png", ".gif"];

    public string[] AllowedContentTypes { get; init; } = ["image/jpeg", "image/png", "image/gif"];
}
