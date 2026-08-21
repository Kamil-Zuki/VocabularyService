using Microsoft.EntityFrameworkCore;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Services;

public class LemmaService : ILemmaService
{
    private readonly VocabularyServiceContext _context;
    private readonly ILogger<LemmaService> _logger;

    private static readonly Dictionary<string, string> IrregularLemmas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["went"] = "go",
        ["gone"] = "go",
        ["goes"] = "go",
        ["going"] = "go",
        ["ran"] = "run",
        ["running"] = "run",
        ["runs"] = "run",
        ["came"] = "come",
        ["coming"] = "come",
        ["did"] = "do",
        ["done"] = "do",
        ["does"] = "do",
        ["was"] = "be",
        ["were"] = "be",
        ["been"] = "be",
        ["being"] = "be",
        ["has"] = "have",
        ["had"] = "have",
        ["having"] = "have",
        ["mice"] = "mouse",
        ["men"] = "man",
        ["women"] = "woman",
        ["children"] = "child",
        ["feet"] = "foot",
        ["teeth"] = "tooth",
        ["geese"] = "goose",
    };

    public LemmaService(
        VocabularyServiceContext context,
        ILogger<LemmaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public string Normalize(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return string.Empty;

        var normalized = word.Trim().ToLowerInvariant();
        normalized = normalized.Trim('\'', '"', '`', '.', ',', '!', '?', ':', ';', '(', ')', '[', ']', '{', '}');

        if (IrregularLemmas.TryGetValue(normalized, out var irregular))
            return irregular;

        if (normalized.EndsWith("ies", StringComparison.Ordinal) && normalized.Length > 3)
            return normalized[..^3] + "y";

        if (normalized.EndsWith("ied", StringComparison.Ordinal) && normalized.Length > 3)
            return normalized[..^3] + "y";

        if (normalized.EndsWith("ing", StringComparison.Ordinal) && normalized.Length > 4)
        {
            var stem = normalized[..^3];
            if (stem.Length >= 2 && stem[^1] == stem[^2] && IsConsonant(stem[^1]))
                stem = stem[..^1];

            if (!stem.EndsWith('e') && LooksLikeVerbStemNeedingTrailingE(stem))
                return stem + "e";

            return stem;
        }

        if (normalized.EndsWith("ed", StringComparison.Ordinal) && normalized.Length > 3)
        {
            var stem = normalized[..^2];
            if (stem.Length >= 2 && stem[^1] == stem[^2] && IsConsonant(stem[^1]))
                stem = stem[..^1];

            if (stem.EndsWith('i'))
                return stem[..^1] + "y";

            if (!stem.EndsWith('e') && LooksLikeVerbStemNeedingTrailingE(stem))
                return stem + "e";

            return stem;
        }

        if (normalized.EndsWith("es", StringComparison.Ordinal) && normalized.Length > 3)
        {
            var stem = normalized[..^2];
            if (normalized.EndsWith("ses", StringComparison.Ordinal)
                || normalized.EndsWith("xes", StringComparison.Ordinal)
                || normalized.EndsWith("zes", StringComparison.Ordinal)
                || normalized.EndsWith("ches", StringComparison.Ordinal)
                || normalized.EndsWith("shes", StringComparison.Ordinal))
            {
                return stem;
            }
        }

        if (normalized.EndsWith('s') && normalized.Length > 2 && !normalized.EndsWith("ss", StringComparison.Ordinal))
            return normalized[..^1];

        return normalized;
    }

    public async Task<ProjectLemma?> ResolveForCardAsync(
        Guid projectId,
        string targetWord,
        Guid? mainCardId = null,
        CancellationToken cancellationToken = default)
    {
        var lemmaText = Normalize(targetWord);
        if (string.IsNullOrWhiteSpace(lemmaText))
            return null;

        var existing = _context.ProjectLemmas.Local
            .FirstOrDefault(lemma => lemma.ProjectId == projectId && lemma.Text == lemmaText)
            ?? await _context.ProjectLemmas
            .FirstOrDefaultAsync(
                lemma => lemma.ProjectId == projectId && lemma.Text == lemmaText,
                cancellationToken);

        if (existing != null)
        {
            if (!existing.MainCardId.HasValue && mainCardId.HasValue)
            {
                existing.MainCardId = mainCardId;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            return existing;
        }

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project == null)
            return null;

        var now = DateTime.UtcNow;
        var created = new ProjectLemma
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Text = lemmaText,
            PosTag = null,
            Status = "LEARNING",
            MainCardId = mainCardId,
            UpdatedAt = now
        };

        _context.ProjectLemmas.Add(created);
        project.Stats ??= new ProjectStats();
        project.Stats.TotalLemmas += 1;

        _logger.LogInformation(
            "Created project lemma {LemmaId} for project {ProjectId}: {LemmaText}",
            created.Id,
            projectId,
            lemmaText);

        return created;
    }

    private static bool LooksLikeVerbStemNeedingTrailingE(string stem)
    {
        if (stem.Length < 3)
            return false;

        return stem.EndsWith("at", StringComparison.Ordinal)
            || stem.EndsWith("it", StringComparison.Ordinal)
            || stem.EndsWith("ov", StringComparison.Ordinal)
            || stem.EndsWith("iz", StringComparison.Ordinal)
            || stem.EndsWith("ak", StringComparison.Ordinal);
    }

    private static bool IsConsonant(char value)
    {
        return value is not ('a' or 'e' or 'i' or 'o' or 'u' or 'y');
    }
}
