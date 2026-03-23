namespace EduConnect.Application.Interfaces;

public interface IGeminiApiService
{
    Task<string> GenerateTextAsync(
        string userMessage,
        IReadOnlyCollection<(string Role, string Content)>? history = null,
        CancellationToken cancellationToken = default);

    Task<string> AnalyzeImageAsync(
        byte[] imageBytes,
        string mimeType,
        string prompt,
        CancellationToken cancellationToken = default);
}
