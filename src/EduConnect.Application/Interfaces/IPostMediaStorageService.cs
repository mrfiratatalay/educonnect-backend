namespace EduConnect.Application.Interfaces;

public interface IPostMediaStorageService
{
    Task<string> SaveAsync(
        Guid userId,
        Stream fileStream,
        string? fileName,
        string? contentType,
        CancellationToken cancellationToken = default);
}
