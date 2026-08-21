using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using VocabularyService.Data;
using VocabularyService.Dtos.Text;
using VocabularyService.Helpers;

namespace VocabularyService.Services;

public class TextService : ITextService
{
    private readonly VocabularyServiceContext _context;
    private readonly ILogger<TextService> _logger;

    private static readonly Regex WordRegex = new(@"\b\w+\b", RegexOptions.Compiled);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "from", "as", "is", "was", "are", "were",
        "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "should", "could", "may", "might", "must", "can",
    };

    private sealed record TermStatusRow(string NormalizedText, string Type, string Status, Guid ProjectTermId);

    public TextService(VocabularyServiceContext context, ILogger<TextService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AnalyzeTextResponseDto> AnalyzeTextAsync(
        Guid userId,
        AnalyzeTextRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.UserId == userId, cancellationToken);

        if (project == null)
            throw new InvalidOperationException($"Project {request.ProjectId} not found or access denied");

        var tokens = TokenizeText(request.Text);
        var wordTokens = tokens.Where(t => t.Type == TokenType.Word).ToList();
        var distinctNormForms = wordTokens
            .Select(t => TermNormalizer.Normalize(t.Text))
            .Where(z => z.Length > 0)
            .Distinct()
            .ToList();

        var statusRows = await LoadTermStatusesAsync(
            userId,
            request.ProjectId,
            cancellationToken);

        var wordLookup = statusRows
            .Where(r => r.Type == "WORD")
            .GroupBy(r => r.NormalizedText, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        foreach (var token in wordTokens)
        {
            var norm = TermNormalizer.Normalize(token.Text);
            token.TermText = norm.Length > 0 ? norm : null;

            if (wordLookup.TryGetValue(norm, out var row))
            {
                token.Status = MapDbStatus(row.Status);
                token.ProjectTermId = row.ProjectTermId;
                continue;
            }

            token.Status = norm.Length > 0 && StopWords.Contains(norm)
                ? TokenStatus.Known
                : TokenStatus.New;
        }

        var phrases = FindPhraseSpans(tokens, statusRows.Where(r => r.Type == "PHRASE").ToList());

        var uniqueSurfaceForms = distinctNormForms.Count;
        var knownWords = wordTokens.Count(t => t.Status == TokenStatus.Known || t.Status == TokenStatus.Ignored);
        var learningWords = wordTokens.Count(t => t.Status == TokenStatus.Learning);
        var newWords = wordTokens.Count(t => t.Status == TokenStatus.New);

        var stats = new TextAnalysisStatsDto
        {
            UniqueWords = uniqueSurfaceForms,
            KnownWordsCount = knownWords,
            LearningWordsCount = learningWords,
            NewWordsCount = newWords,
            KnownPercentage = uniqueSurfaceForms > 0 ? (double)knownWords / uniqueSurfaceForms : 0.0,
        };

        _logger.LogInformation(
            "Analyzed text (term-first) for project {ProjectId}: unique surface forms={Unique}, phrases={Phrases}",
            request.ProjectId, uniqueSurfaceForms, phrases.Count);

        return new AnalyzeTextResponseDto
        {
            Tokens = tokens,
            Phrases = phrases,
            Stats = stats,
        };
    }

    private static TokenStatus MapDbStatus(string status) =>
        status.Trim().ToUpperInvariant() switch
        {
            "SAVED" or "LINGQ" => TokenStatus.Learning,
            "KNOWN" => TokenStatus.Known,
            "IGNORED" => TokenStatus.Ignored,
            "NEW" => TokenStatus.New,
            _ => TokenStatus.New,
        };

    private async Task<List<TermStatusRow>> LoadTermStatusesAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct)
    {
        return await (
                from pt in _context.ProjectTerms.AsNoTracking()
                join uts in _context.UserTermStatuses.AsNoTracking()
                    on pt.Id equals uts.ProjectTermId
                where uts.UserId == userId
                      && pt.ProjectId == projectId
                      && (pt.Type == "WORD" || pt.Type == "PHRASE")
                select new TermStatusRow(pt.NormalizedText, pt.Type, uts.Status, pt.Id))
            .ToListAsync(ct);
    }

    private static List<TextPhraseDto> FindPhraseSpans(
        List<TextTokenDto> tokens,
        List<TermStatusRow> phraseTerms)
    {
        if (phraseTerms.Count == 0)
            return [];

        var wordIndexes = new List<int>();
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Type == TokenType.Word)
                wordIndexes.Add(i);
        }

        var spans = new List<TextPhraseDto>();
        var claimed = new HashSet<int>();

        foreach (var phrase in phraseTerms.OrderByDescending(p => p.NormalizedText.Length))
        {
            var phraseWords = TermNormalizer.Normalize(phrase.NormalizedText)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (phraseWords.Length < 2)
                continue;

            for (var w = 0; w <= wordIndexes.Count - phraseWords.Length; w++)
            {
                var match = true;
                for (var j = 0; j < phraseWords.Length; j++)
                {
                    var tokenIdx = wordIndexes[w + j];
                    var norm = TermNormalizer.Normalize(tokens[tokenIdx].Text);
                    if (!string.Equals(norm, phraseWords[j], StringComparison.Ordinal))
                    {
                        match = false;
                        break;
                    }
                }

                if (!match)
                    continue;

                var startIndex = wordIndexes[w];
                var endIndex = wordIndexes[w + phraseWords.Length - 1];
                if (Enumerable.Range(startIndex, endIndex - startIndex + 1).Any(claimed.Contains))
                    continue;

                for (var i = startIndex; i <= endIndex; i++)
                    claimed.Add(i);

                var text = string.Concat(tokens.Skip(startIndex).Take(endIndex - startIndex + 1).Select(t => t.Text));
                spans.Add(new TextPhraseDto
                {
                    StartIndex = startIndex,
                    EndIndex = endIndex,
                    Text = text,
                    Status = MapDbStatus(phrase.Status),
                    ProjectTermId = phrase.ProjectTermId,
                });
            }
        }

        return spans.OrderBy(p => p.StartIndex).ToList();
    }

    private List<TextTokenDto> TokenizeText(string text)
    {
        var tokens = new List<TextTokenDto>();
        var currentIndex = 0;

        while (currentIndex < text.Length)
        {
            var charCurrent = text[currentIndex];

            if (char.IsWhiteSpace(charCurrent))
            {
                tokens.Add(new TextTokenDto
                {
                    Text = charCurrent.ToString(),
                    Type = TokenType.Space,
                    Status = TokenStatus.New,
                });
                currentIndex++;
            }
            else if (char.IsPunctuation(charCurrent) || char.IsSymbol(charCurrent))
            {
                tokens.Add(new TextTokenDto
                {
                    Text = charCurrent.ToString(),
                    Type = TokenType.Punctuation,
                    Status = TokenStatus.New,
                });
                currentIndex++;
            }
            else
            {
                var wordMatch = WordRegex.Match(text, currentIndex);
                if (wordMatch.Success && wordMatch.Index == currentIndex)
                {
                    var word = wordMatch.Value;

                    tokens.Add(new TextTokenDto
                    {
                        Text = word,
                        Type = TokenType.Word,
                        Status = TokenStatus.New,
                        Lemma = null,
                    });

                    currentIndex = wordMatch.Index + wordMatch.Length;
                }
                else
                    currentIndex++;
            }
        }

        return tokens;
    }
}
