namespace VocabularyService.Options;

/// <summary>
/// Configuration for local Ollama LLM integration.
/// </summary>
public class OllamaOptions
{
    public const string SectionName = "Ollama";

    /// <summary>
    /// Base URL of the Ollama API (default: http://localhost:11434).
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Model name (e.g. qwen2.5-coder:3b).
    /// </summary>
    public string Model { get; set; } = "qwen2.5-coder:3b";

    /// <summary>
    /// Timeout in seconds for LLM requests.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}
