using System.Text;
using Microsoft.Extensions.Options;
using OllamaSharp;
using VocabularyService.Options;

namespace VocabularyService.Services;

/// <summary>
/// Ollama API client for local LLM text generation.
/// </summary>
public class OllamaClient : IOllamaClient
{
    private readonly OllamaApiClient _ollama;
    private readonly ILogger<OllamaClient> _logger;

    public OllamaClient(IOptions<OllamaOptions> options, ILogger<OllamaClient> logger)
    {
        var opts = options.Value;
        _ollama = new OllamaApiClient(new Uri(opts.BaseUrl), opts.Model);
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var chat = new Chat(_ollama);
        var sb = new StringBuilder();
        await foreach (var token in chat.SendAsync(prompt).WithCancellation(cancellationToken))
            sb.Append(token);
        return sb.ToString().Trim();
    }
}
