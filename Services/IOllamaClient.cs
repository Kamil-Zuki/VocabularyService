namespace VocabularyService.Services;

/// <summary>
/// Abstraction for LLM text generation via Ollama.
/// </summary>
public interface IOllamaClient
{
    /// <summary>
    /// Generates a completion for the given prompt.
    /// </summary>
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}
