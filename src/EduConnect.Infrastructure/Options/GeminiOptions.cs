namespace EduConnect.Infrastructure.Options;

public sealed class GeminiOptions
{
    public const string SectionName = "GeminiSettings";

    public string ApiKey { get; set; } = string.Empty;

    public string ChatModel { get; set; } = "gemini-2.0-flash";

    public string VisionModel { get; set; } = "gemini-2.5-flash-preview-04-17";

    public int MaxOutputTokens { get; set; } = 2048;

    public float Temperature { get; set; } = 0.7f;
}
