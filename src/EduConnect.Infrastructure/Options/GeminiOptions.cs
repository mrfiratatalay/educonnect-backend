namespace EduConnect.Infrastructure.Options;

public sealed class GeminiOptions
{
    public const string SectionName = "GeminiSettings";

    public string ApiKey { get; set; } = string.Empty;

    public string ChatModel { get; set; } = "gemini-2.5-flash";

    public string VisionModel { get; set; } = "gemini-2.5-flash";

    public int MaxOutputTokens { get; set; } = 2048;

    public float Temperature { get; set; } = 0.7f;
}
