using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Dtos.AI;

namespace VocabularyService.Services;

public class AIService : IAIService
{
    private readonly ILogger<AIService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IOllamaClient _ollama;
    private const int CacheExpirationHours = 24;

    public AIService(
        ILogger<AIService> logger,
        IMemoryCache cache,
        IOllamaClient ollama)
    {
        _logger = logger;
        _cache = cache;
        _ollama = ollama;
    }

    public async Task<GenerateContextResponseDto> GenerateContextAsync(
        Guid userId,
        GenerateContextRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"context_{request.TargetWord.ToLowerInvariant()}_{request.Language}_{request.UserLevel}";
        if (_cache.TryGetValue(cacheKey, out GenerateContextResponseDto? cachedResponse) && cachedResponse is not null)
        {
            _logger.LogInformation("Returning cached context for word {Word}", request.TargetWord);
            return cachedResponse;
        }

        var jsonFormat = "{\"suggestions\":[{\"sentence\":\"...\",\"translation\":\"...\"}, ...]}";
        var prompt = $"Generate {request.Count} example sentences in {request.Language} for CEFR level {request.UserLevel}. Each sentence must contain the word \"{request.TargetWord}\". Return ONLY valid JSON: {jsonFormat}. Translation: English. No other text.";
        var llmResponse = await _ollama.GenerateAsync(prompt, cancellationToken);

        var suggestions = ParseContextSuggestions(llmResponse, request.TargetWord);

        var response = new GenerateContextResponseDto { Suggestions = suggestions };
        _cache.Set(cacheKey, response, TimeSpan.FromHours(CacheExpirationHours));

        _logger.LogInformation("Generated {Count} context suggestions for word {Word}", suggestions.Count, request.TargetWord);
        return response;
    }

    public async Task<ExplainGrammarResponseDto> ExplainGrammarAsync(
        Guid userId,
        ExplainGrammarRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var prompt = $"Explain why '{request.TargetWord}' is used in this sentence: \"{request.Sentence}\". Explain in {request.UserNativeLanguage}. Keep it short (2-4 sentences). State the grammar topic at the end in square brackets, e.g. [Passé Composé].";
        var llmResponse = await _ollama.GenerateAsync(prompt, cancellationToken);

        var (explanation, topic) = ParseGrammarResponse(llmResponse);
        var response = new ExplainGrammarResponseDto
        {
            Explanation = explanation,
            RelatedTopic = topic
        };

        _logger.LogInformation("Generated grammar explanation for word {Word} in sentence {Sentence}",
            request.TargetWord, request.Sentence);
        return response;
    }

    private static List<ContextSuggestionDto> ParseContextSuggestions(string llmResponse, string targetWord)
    {
        var suggestions = new List<ContextSuggestionDto>();

        try
        {
            var json = ExtractJson(llmResponse);
            if (string.IsNullOrEmpty(json)) throw new JsonException("No JSON found");

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("suggestions", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var sentence = item.TryGetProperty("sentence", out var s) ? s.GetString() ?? "" : "";
                    var translation = item.TryGetProperty("translation", out var t) ? t.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(sentence)) continue;

                    var idx = FindTargetIndex(sentence, targetWord);
                    suggestions.Add(new ContextSuggestionDto
                    {
                        Sentence = sentence,
                        Translation = translation,
                        TargetWord = targetWord,
                        TargetIndex = idx
                    });
                }
            }
        }
        catch
        {
            foreach (var line in llmResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var clean = line.TrimStart('-', '*', ' ', '\t').Trim();
                if (clean.Length > 10 && clean.Contains(targetWord, StringComparison.OrdinalIgnoreCase))
                    suggestions.Add(new ContextSuggestionDto
                    {
                        Sentence = clean,
                        Translation = "",
                        TargetWord = targetWord,
                        TargetIndex = FindTargetIndex(clean, targetWord)
                    });
            }
        }

        if (suggestions.Count == 0)
            suggestions.Add(new ContextSuggestionDto
            {
                Sentence = llmResponse.Length > 200 ? llmResponse[..200] + "..." : llmResponse,
                Translation = "",
                TargetWord = targetWord,
                TargetIndex = FindTargetIndex(llmResponse, targetWord)
            });

        return suggestions;
    }

    private static (string Explanation, string? Topic) ParseGrammarResponse(string llmResponse)
    {
        var topic = (string?)null;
        var match = Regex.Match(llmResponse, @"\[([^\]]+)\]\s*$");
        if (match.Success)
        {
            topic = match.Groups[1].Value.Trim();
            llmResponse = llmResponse[..match.Index].Trim();
        }
        var explanation = llmResponse.Length > 0 ? llmResponse : "No explanation generated.";
        return (explanation, topic);
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        if (start < 0) return "";
        var depth = 0;
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            if (text[i] == '}')
            {
                depth--;
                if (depth == 0) return text[start..(i + 1)];
            }
        }
        return "";
    }

    private static TargetIndex FindTargetIndex(string sentence, string targetWord)
    {
        var idx = sentence.IndexOf(targetWord, StringComparison.OrdinalIgnoreCase);
        return idx >= 0
            ? new TargetIndex { Start = idx, Len = targetWord.Length }
            : new TargetIndex { Start = 0, Len = targetWord.Length };
    }
}
