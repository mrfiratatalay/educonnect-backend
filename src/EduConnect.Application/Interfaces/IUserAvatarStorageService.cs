namespace EduConnect.Application.Interfaces;

public interface IUserAvatarStorageService
{
    Task<string> SaveAvatarAsync(
        Guid userId,
        Stream fileStream,
        string? fileName,
        string? contentType,
        CancellationToken cancellationToken = default);

    Task<string> SaveCoverAsync(
        Guid userId,
        Stream fileStream,
        string? fileName,
        string? contentType,
        CancellationToken cancellationToken = default);
}
