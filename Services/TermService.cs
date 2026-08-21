using Microsoft.EntityFrameworkCore;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Domain;
using VocabularyService.Dtos.Terms;
using VocabularyService.Helpers;

namespace VocabularyService.Services;

public class TermService : ITermService
{
    private readonly VocabularyServiceContext _db;
    private readonly ILogger<TermService> _logger;

    public TermService(VocabularyServiceContext db, ILogger<TermService> logger)
    {
        _db = db;
        _logger = logger;
    }

    private static string ResolveType(string? type) =>
        string.IsNullOrWhiteSpace(type) ? "WORD" : type.Trim().ToUpperInvariant();

    private async Task EnsureProjectOwnedByAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var exists = await _db.Projects.AnyAsync(p => p.Id == projectId && p.UserId == userId, ct);
        if (!exists)
            throw new KeyNotFoundException($"Project {projectId} not found");
    }

    private async Task<ProjectTerm> ResolveOrCreateTrackedTermAsync(
        Guid projectId,
        string surfaceText,
        string type,
        string? languageHint,
        CancellationToken ct)
    {
        _ = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new KeyNotFoundException($"Project {projectId} missing");

        var normalized = TermNormalizer.Normalize(surfaceText);
        if (string.IsNullOrEmpty(normalized))
            throw new ArgumentException("term_text empty after normalize");

        var projectMeta = await _db.Projects.AsNoTracking().FirstAsync(p => p.Id == projectId, ct);
        var lang = languageHint ?? projectMeta.SourceLang;

        var term = await _db.ProjectTerms.FirstOrDefaultAsync(
            t => t.ProjectId == projectId && t.NormalizedText == normalized && t.Type == type,
            ct);

        // В одной транзакции Add без SaveChanges: повторный запрос к БД не видит новую строку — проверяем Local.
        term ??= _db.ProjectTerms.Local.FirstOrDefault(
            t => t.ProjectId == projectId && t.NormalizedText == normalized && t.Type == type);

        if (term != null)
            return term;

        term = new ProjectTerm
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Text = surfaceText.Trim(),
            NormalizedText = normalized,
            Type = type,
            Language = lang,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _db.ProjectTerms.AddAsync(term, ct);
        return term;
    }

    private static void TouchStatus(UserTermStatus row)
    {
        row.UpdatedAt = DateTime.UtcNow;
        row.LastSeenAt = DateTime.UtcNow;
    }

    public async Task<UserTermStatus> CreateOrUpdateAsync(
        Guid userId,
        Guid projectId,
        string surfaceText,
        string typeRaw,
        string? languageHint,
        string? statusHint,
        string? meaning,
        string? firstSentence,
        string? firstSourceTitle,
        string? firstSourceUrl,
        CancellationToken cancellationToken = default)
    {
        await EnsureProjectOwnedByAsync(userId, projectId, cancellationToken);

        var type = ResolveType(typeRaw);
        var term = await ResolveOrCreateTrackedTermAsync(projectId, surfaceText, type, languageHint, cancellationToken);

        var targetStatus = string.IsNullOrWhiteSpace(statusHint)
            ? "SAVED"
            : statusHint.Trim().ToUpperInvariant();

        // SAVED — сохранённый термин с переводом (ранее в коде/доках «LINGQ»). Принимаем legacy.
        targetStatus = targetStatus switch
        {
            "NEW" => "NEW",
            "SAVED" or "LINGQ" or "LEARNING" => "SAVED",
            "KNOWN" => "KNOWN",
            "IGNORED" => "IGNORED",
            _ => "SAVED",
        };

        var row = await _db.UserTermStatuses.FirstOrDefaultAsync(
            r => r.UserId == userId && r.ProjectTermId == term.Id,
            cancellationToken);

        if (row == null)
        {
            row = new UserTermStatus
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProjectId = projectId,
                ProjectTermId = term.Id,
                Status = targetStatus,
                Meaning = meaning,
                FirstSentence = firstSentence,
                FirstSourceTitle = firstSourceTitle,
                FirstSourceUrl = firstSourceUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
            };
            await _db.UserTermStatuses.AddAsync(row, cancellationToken);
        }
        else
        {
            row.Status = targetStatus;

            if (!string.IsNullOrWhiteSpace(meaning))
                row.Meaning = meaning;

            if (!string.IsNullOrWhiteSpace(firstSentence))
                row.FirstSentence = firstSentence;

            if (!string.IsNullOrWhiteSpace(firstSourceTitle))
                row.FirstSourceTitle = firstSourceTitle;

            if (!string.IsNullOrWhiteSpace(firstSourceUrl))
                row.FirstSourceUrl = firstSourceUrl;

            TouchStatus(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task<UserTermStatus> MarkKnownAsync(
        Guid userId,
        Guid projectId,
        string surfaceText,
        string typeRaw,
        string? languageHint,
        CancellationToken cancellationToken = default)
    {
        await EnsureProjectOwnedByAsync(userId, projectId, cancellationToken);

        var row = await MarkKnownCoreAsync(userId, projectId, surfaceText, typeRaw, languageHint, cancellationToken).ConfigureAwait(false);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return row;
    }

    /// <summary>Marks known without <see cref="DbContext.SaveChangesAsync"/> — use inside a transaction for bulk updates.</summary>
    private async Task<UserTermStatus> MarkKnownCoreAsync(
        Guid userId,
        Guid projectId,
        string surfaceText,
        string typeRaw,
        string? languageHint,
        CancellationToken cancellationToken)
    {
        var type = ResolveType(typeRaw);
        var term = await ResolveOrCreateTrackedTermAsync(projectId, surfaceText, type, languageHint, cancellationToken).ConfigureAwait(false);

        var row = await _db.UserTermStatuses.FirstOrDefaultAsync(
            r => r.UserId == userId && r.ProjectTermId == term.Id,
            cancellationToken).ConfigureAwait(false);

        if (row == null)
        {
            row = new UserTermStatus
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProjectId = projectId,
                ProjectTermId = term.Id,
                Status = "KNOWN",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
            };
            await _db.UserTermStatuses.AddAsync(row, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            row.Status = "KNOWN";
            TouchStatus(row);
        }

        return row;
    }

    public async Task<UserTermStatus> IgnoreAsync(
        Guid userId,
        Guid projectId,
        string surfaceText,
        string typeRaw,
        string? languageHint,
        CancellationToken cancellationToken = default)
    {
        await EnsureProjectOwnedByAsync(userId, projectId, cancellationToken);

        var type = ResolveType(typeRaw);
        var term = await ResolveOrCreateTrackedTermAsync(projectId, surfaceText, type, languageHint, cancellationToken);

        var row = await _db.UserTermStatuses.FirstOrDefaultAsync(
            r => r.UserId == userId && r.ProjectTermId == term.Id,
            cancellationToken);

        if (row == null)
        {
            row = new UserTermStatus
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProjectId = projectId,
                ProjectTermId = term.Id,
                Status = "IGNORED",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
            };
            await _db.UserTermStatuses.AddAsync(row, cancellationToken);
        }
        else
        {
            row.Status = "IGNORED";
            TouchStatus(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task<int> BulkMarkKnownAsync(
        Guid userId,
        Guid projectId,
        IReadOnlyList<BulkMarkKnownItemDto> items,
        string? languageHint,
        CancellationToken cancellationToken = default)
    {
        await EnsureProjectOwnedByAsync(userId, projectId, cancellationToken).ConfigureAwait(false);

        // NpgsqlRetryingExecutionStrategy does not allow BeginTransaction outside CreateExecutionStrategy.ExecuteAsync.
        var strategy = _db.Database.CreateExecutionStrategy();
        var resultCount = 0;
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var count = 0;
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.SurfaceText))
                        continue;

                    var type = ResolveType(item.Type);
                    await MarkKnownCoreAsync(userId, projectId, item.SurfaceText, type, languageHint, cancellationToken)
                        .ConfigureAwait(false);
                    count++;
                }

                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                resultCount = count;
                _logger.LogInformation("BulkMarkKnown processed {Count} items for project {ProjectId}", count, projectId);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);

        return resultCount;
    }

    public async Task<(ProjectTerm Term, UserTermStatus? Status)> GetDetailsAsync(
        Guid userId,
        Guid projectId,
        string surfaceText,
        string typeRaw,
        CancellationToken cancellationToken = default)
    {
        await EnsureProjectOwnedByAsync(userId, projectId, cancellationToken);

        var type = ResolveType(typeRaw);
        var normalized = TermNormalizer.Normalize(surfaceText);

        var term = await _db.ProjectTerms.FirstOrDefaultAsync(
            t => t.ProjectId == projectId && t.NormalizedText == normalized && t.Type == type,
            cancellationToken);

        if (term == null)
            throw new KeyNotFoundException("Term not found");

        var row = await _db.UserTermStatuses.FirstOrDefaultAsync(
            r => r.UserId == userId && r.ProjectTermId == term.Id,
            cancellationToken);

        return (term, row);
    }

    public async Task<(ProjectTerm? Term, List<Card> Cards)> SearchDuplicatesAsync(
        Guid userId,
        Guid projectId,
        string surfaceText,
        string typeRaw,
        CancellationToken cancellationToken = default)
    {
        await EnsureProjectOwnedByAsync(userId, projectId, cancellationToken);

        var type = ResolveType(typeRaw);
        var normalized = TermNormalizer.Normalize(surfaceText);

        var term = await _db.ProjectTerms.FirstOrDefaultAsync(
            t => t.ProjectId == projectId && t.NormalizedText == normalized && t.Type == type,
            cancellationToken);

        var cards = await _db.Cards
            .Include(c => c.Deck)
            .Include(c => c.Note)
            .Include(c => c.UserCardProgresses.Where(p => p.UserId == userId))
            .Where(c => c.Deck.ProjectId == projectId && c.CreatorId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(400)
            .ToListAsync(cancellationToken);

        var filtered = cards
            .Where(c =>
            {
                var w = c.Note != null ? NoteFieldMapHelper.GetWord(c.Note.FieldValues) : string.Empty;
                return term != null && c.ProjectTermId == term.Id
                       || TermNormalizer.Normalize(w) == normalized;
            })
            .Take(25)
            .ToList();

        return (term, filtered);
    }

    public async Task<(IReadOnlyList<ProjectTermListRow> Items, int TotalCount)> ListProjectTermsAsync(
        Guid userId,
        Guid projectId,
        string? statusFilter,
        string? typeFilter,
        string? searchQuery,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await EnsureProjectOwnedByAsync(userId, projectId, cancellationToken);

        var take = pageSize <= 0 ? 50 : Math.Min(pageSize, 100);

        var query =
            from uts in _db.UserTermStatuses.AsNoTracking()
            join pt in _db.ProjectTerms.AsNoTracking() on uts.ProjectTermId equals pt.Id
            where uts.UserId == userId && uts.ProjectId == projectId && pt.ProjectId == projectId
            select new { uts, pt };

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            var sf = statusFilter.Trim().ToUpperInvariant();
            query = sf switch
            {
                "SAVED" => query.Where(x =>
                    x.uts.Status == "SAVED" || x.uts.Status == "LINGQ" || x.uts.Status == "LEARNING"),
                "NEW" or "KNOWN" or "IGNORED" => query.Where(x => x.uts.Status == sf),
                _ => throw new ArgumentException("Invalid status filter", nameof(statusFilter)),
            };
        }

        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            var t = ResolveType(typeFilter);
            query = query.Where(x => x.pt.Type == t);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var q = searchQuery.Trim();
            var ql = q.ToLowerInvariant();
            query = query.Where(x =>
                x.pt.Text.ToLower().Contains(ql) ||
                x.pt.NormalizedText.Contains(ql));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        
        var skip = Math.Max(0, pageNumber - 1) * take;

        var rows = await query
            .OrderByDescending(x => x.uts.UpdatedAt)
            .ThenBy(x => x.pt.Id)
            .Select(x => new
            {
                x.pt.Id,
                x.pt.Text,
                x.pt.NormalizedText,
                x.pt.Type,
                Lang = x.pt.Language ?? "",
                x.uts.Status,
                x.uts.Meaning,
                x.uts.FirstSentence,
                x.uts.FirstSourceTitle,
                x.uts.FirstSourceUrl,
                x.uts.UpdatedAt,
                x.uts.ReadingLevel,
                x.uts.ListeningLevel,
                x.uts.WritingLevel,
                x.uts.SpeakingLevel
            })
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var pageTermIds = rows.Select(r => r.Id).ToList();
        var relatedCardCounts = pageTermIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await (
                from c in _db.Cards.AsNoTracking()
                join d in _db.Decks.AsNoTracking() on c.DeckId equals d.Id
                where c.CreatorId == userId
                      && d.ProjectId == projectId
                      && c.ProjectTermId != null
                      && pageTermIds.Contains(c.ProjectTermId.Value)
                group c by c.ProjectTermId into g
                select new { TermId = g.Key!.Value, Count = g.Count() })
                .ToDictionaryAsync(x => x.TermId, x => x.Count, cancellationToken);

        var items = rows.Select(r => new ProjectTermListRow(
                r.Id,
                r.Text,
                r.NormalizedText,
                r.Type,
                r.Lang,
                r.Status,
                r.Meaning,
                r.FirstSentence,
                r.FirstSourceTitle,
                r.FirstSourceUrl,
                DateTime.SpecifyKind(r.UpdatedAt, DateTimeKind.Utc),
                relatedCardCounts.GetValueOrDefault(r.Id),
                r.ReadingLevel,
                r.ListeningLevel,
                r.WritingLevel,
                r.SpeakingLevel))
            .ToList();

        return (items, totalCount);
    }

    public async Task LinkCardWordTermAsync(Guid userId, Card card, CancellationToken cancellationToken)
    {
        var deck = await _db.Decks.AsNoTracking().FirstAsync(d => d.Id == card.DeckId, cancellationToken);
        var note = card.Note ?? await _db.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == card.NoteId, cancellationToken);
        if (note == null)
            return;

        var word = NoteFieldMapHelper.GetWord(note.FieldValues);
        var norm = TermNormalizer.Normalize(word);
        if (string.IsNullOrEmpty(norm))
            return;

        var termType = word.Contains(' ', StringComparison.Ordinal) ? "PHRASE" : "WORD";
        var term = await ResolveOrCreateTrackedTermAsync(deck.ProjectId, word, termType, null, cancellationToken);

        var translation = NoteFieldMapHelper.GetTranslation(note.FieldValues);
        var expression = NoteFieldMapHelper.GetExpression(note.FieldValues);
        var sourceTitle = NoteFieldMapHelper.GetString(note.FieldValues, SentenceMiningNoteType.SourceTitle);
        var sourceUrl = NoteFieldMapHelper.GetString(note.FieldValues, SentenceMiningNoteType.SourceUrl);

        var row = await _db.UserTermStatuses.FirstOrDefaultAsync(
            r => r.UserId == userId && r.ProjectTermId == term.Id,
            cancellationToken);

        row ??= _db.UserTermStatuses.Local.FirstOrDefault(
            r => r.UserId == userId && r.ProjectTermId == term.Id);

        if (row == null)
        {
            row = new UserTermStatus
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProjectId = deck.ProjectId,
                ProjectTermId = term.Id,
                Status = "SAVED",
                Meaning = string.IsNullOrWhiteSpace(translation) ? null : translation,
                FirstSentence = expression,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
            };

            if (!string.IsNullOrWhiteSpace(sourceTitle))
                row.FirstSourceTitle = sourceTitle;
            if (!string.IsNullOrWhiteSpace(sourceUrl))
                row.FirstSourceUrl = sourceUrl;

            await _db.UserTermStatuses.AddAsync(row, cancellationToken);
        }
        else
        {
            // Preserve manual Reader/Vocabulary decisions; card link must not downgrade KNOWN/IGNORED.
            if (row.Status is not ("KNOWN" or "IGNORED"))
                row.Status = "SAVED";

            if (!string.IsNullOrWhiteSpace(translation))
                row.Meaning = translation;

            if (!string.IsNullOrWhiteSpace(expression) && string.IsNullOrWhiteSpace(row.FirstSentence))
                row.FirstSentence = expression;

            TouchStatus(row);
        }

        card.ProjectTermId = term.Id;
    }

    public async Task<PurgeDemoImportResult> PurgeDemoImportDataAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await EnsureProjectOwnedByAsync(userId, projectId, cancellationToken);

        var cards = await _db.Cards
            .Include(c => c.Note)
            .Include(c => c.Deck)
            .Where(c => c.CreatorId == userId && c.Deck.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var demoCards = cards
            .Where(c => c.Note != null && IsDemoImportNote(c.Note.FieldValues))
            .ToList();

        var cardsDeleted = 0;
        var affectedTermIds = new HashSet<Guid>();

        foreach (var card in demoCards)
        {
            if (card.ProjectTermId.HasValue)
                affectedTermIds.Add(card.ProjectTermId.Value);

            var noteId = card.NoteId;
            _db.Cards.Remove(card);
            if (card.Deck != null)
                card.Deck.CardCount = Math.Max(0, card.Deck.CardCount - 1);

            var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);
            if (note != null)
                _db.Notes.Remove(note);

            cardsDeleted++;
        }

        var demoStatuses = await _db.UserTermStatuses
            .Where(s => s.UserId == userId && s.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var statusesToDelete = demoStatuses.Where(IsDemoImportStatus).ToList();
        foreach (var status in statusesToDelete)
            affectedTermIds.Add(status.ProjectTermId);

        if (statusesToDelete.Count > 0)
            _db.UserTermStatuses.RemoveRange(statusesToDelete);

        await _db.SaveChangesAsync(cancellationToken);

        var termsDeleted = 0;
        foreach (var termId in affectedTermIds)
        {
            var hasStatus = await _db.UserTermStatuses.AnyAsync(
                s => s.ProjectTermId == termId,
                cancellationToken);
            var hasCard = await _db.Cards.AnyAsync(
                c => c.ProjectTermId == termId,
                cancellationToken);
            if (hasStatus || hasCard)
                continue;

            var term = await _db.ProjectTerms.FirstOrDefaultAsync(
                t => t.Id == termId && t.ProjectId == projectId,
                cancellationToken);
            if (term == null)
                continue;

            _db.ProjectTerms.Remove(term);
            termsDeleted++;
        }

        if (termsDeleted > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "PurgeDemoImport project {ProjectId} user {UserId}: cards={Cards} statuses={Statuses} terms={Terms}",
            projectId,
            userId,
            cardsDeleted,
            statusesToDelete.Count,
            termsDeleted);

        return new PurgeDemoImportResult(cardsDeleted, statusesToDelete.Count, termsDeleted);
    }

    private static bool IsDemoImportNote(Dictionary<string, NoteFieldValue>? fieldValues)
    {
        if (fieldValues == null)
            return false;

        var expression = NoteFieldMapHelper.GetExpression(fieldValues);
        var translation = NoteFieldMapHelper.GetTranslation(fieldValues);
        return expression.StartsWith("[Import demo #", StringComparison.Ordinal)
               && translation.StartsWith("демо-", StringComparison.Ordinal);
    }

    private static bool IsDemoImportStatus(UserTermStatus status)
    {
        return status.Meaning != null
               && status.Meaning.StartsWith("демо-", StringComparison.Ordinal)
               && status.FirstSentence != null
               && status.FirstSentence.Contains("[Import demo", StringComparison.Ordinal);
    }
}
