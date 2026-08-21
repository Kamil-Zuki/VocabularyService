using Microsoft.EntityFrameworkCore;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Domain;
using VocabularyService.Dtos.Cards;
using VocabularyService.Helpers;

namespace VocabularyService.Services;

public class CardService : ICardService
{
    private readonly VocabularyServiceContext _context;
    private readonly ITermService _termService;
    private readonly IMediaService _mediaService;
    private readonly INoteTypeService _noteTypeService;
    private readonly ILogger<CardService> _logger;

    public CardService(
        VocabularyServiceContext context,
        ITermService termService,
        IMediaService mediaService,
        INoteTypeService noteTypeService,
        ILogger<CardService> logger)
    {
        _context = context;
        _termService = termService;
        _mediaService = mediaService;
        _noteTypeService = noteTypeService;
        _logger = logger;
    }

    public async Task<Card> CreateCardAsync(CreateCardDto dto, CancellationToken cancellationToken = default)
    {
        var deck = await _context.Decks
            .FirstOrDefaultAsync(d => d.Id == dto.DeckId, cancellationToken);

        if (deck == null)
            throw new KeyNotFoundException($"Deck {dto.DeckId} not found");

        if (deck.OwnerId != dto.UserId)
            throw new UnauthorizedAccessException("You don't have permission to add cards to this deck");

        var map = NoteFieldMapHelper.NormalizeSentenceMiningMap(dto.FieldValues);
        var expr = NoteFieldMapHelper.GetExpression(map);
        var word = NoteFieldMapHelper.GetWord(map);
        NoteFieldMapHelper.CalculateTargetIndex(expr, word);

        var (noteType, defaultTemplate) =
            await _noteTypeService.EnsureSentenceMiningAsync(deck.ProjectId, cancellationToken).ConfigureAwait(false);

        var noteId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var note = new Note
        {
            Id = noteId,
            DeckId = dto.DeckId,
            CreatorId = dto.UserId,
            NoteTypeId = noteType.Id,
            FieldValues = map,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var searchDoc = NoteFieldMapHelper.BuildSearchDocument(map);
        var card = new Card
        {
            Id = Guid.NewGuid(),
            DeckId = dto.DeckId,
            CreatorId = dto.UserId,
            NoteId = noteId,
            SearchDocument = searchDoc,
            CardTemplateId = defaultTemplate.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _context.Notes.Add(note);
                _context.Cards.Add(card);

                await _termService.LinkCardWordTermAsync(dto.UserId, card, cancellationToken);

                note.ProjectTermId = card.ProjectTermId;

                deck.CardCount++;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        await _context.Entry(card).Reference(c => c.Note).LoadAsync(cancellationToken);
        await _context.Entry(card).Reference(c => c.CardTemplate).LoadAsync(cancellationToken);

        _logger.LogInformation("Card {CardId} created in deck {DeckId}", card.Id, dto.DeckId);

        await FillResolvedCardMediaAsync(card, cancellationToken).ConfigureAwait(false);
        return card;
    }

    public async Task<Card> CreateCardAsDeckOwnerAsync(Guid deckOwnerUserId, CreateCardDto dto, CancellationToken cancellationToken = default)
    {
        var deck = await _context.Decks
            .FirstOrDefaultAsync(d => d.Id == dto.DeckId, cancellationToken);

        if (deck == null)
            throw new KeyNotFoundException($"Deck {dto.DeckId} not found");

        if (deck.OwnerId != deckOwnerUserId)
            throw new UnauthorizedAccessException("Only the deck owner can create cards this way");

        var map = NoteFieldMapHelper.NormalizeSentenceMiningMap(dto.FieldValues);
        var expr = NoteFieldMapHelper.GetExpression(map);
        var word = NoteFieldMapHelper.GetWord(map);
        NoteFieldMapHelper.CalculateTargetIndex(expr, word);

        var (noteType, defaultTemplate) =
            await _noteTypeService.EnsureSentenceMiningAsync(deck.ProjectId, cancellationToken).ConfigureAwait(false);

        var noteId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var note = new Note
        {
            Id = noteId,
            DeckId = dto.DeckId,
            CreatorId = dto.UserId,
            NoteTypeId = noteType.Id,
            FieldValues = map,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var searchDoc = NoteFieldMapHelper.BuildSearchDocument(map);
        var card = new Card
        {
            Id = Guid.NewGuid(),
            DeckId = dto.DeckId,
            CreatorId = dto.UserId,
            NoteId = noteId,
            SearchDocument = searchDoc,
            CardTemplateId = defaultTemplate.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _context.Notes.Add(note);
                _context.Cards.Add(card);

                await _termService.LinkCardWordTermAsync(dto.UserId, card, cancellationToken);

                note.ProjectTermId = card.ProjectTermId;

                deck.CardCount++;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        await _context.Entry(card).Reference(c => c.Note).LoadAsync(cancellationToken);
        await _context.Entry(card).Reference(c => c.CardTemplate).LoadAsync(cancellationToken);

        await FillResolvedCardMediaAsync(card, cancellationToken).ConfigureAwait(false);
        return card;
    }

    public async Task<CheckCardDuplicatesResponseDto> CheckDuplicatesAsync(
        Guid userId,
        CheckCardDuplicatesRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId && p.UserId == userId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException($"Project {dto.ProjectId} not found");

        var normalizedSurface = TermNormalizer.Normalize(dto.TermText);
        if (string.IsNullOrWhiteSpace(normalizedSurface))
        {
            return new CheckCardDuplicatesResponseDto
            {
                IsDuplicate = false,
                NormalizedSurface = normalizedSurface,
                ExistingCards = []
            };
        }

        const string wordType = "WORD";
        var projectTerm = await _context.ProjectTerms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ProjectId == dto.ProjectId && t.NormalizedText == normalizedSurface && t.Type == wordType,
                cancellationToken);

        var candidateCards = await _context.Cards
            .Include(c => c.Deck)
            .Include(c => c.Note)
            .Include(c => c.UserCardProgresses.Where(progress => progress.UserId == userId))
            .Where(c => c.CreatorId == userId && c.Deck.ProjectId == dto.ProjectId)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(320)
            .ToListAsync(cancellationToken);

        var duplicates = candidateCards.Where(c =>
        {
            var noteMap = c.Note?.FieldValues;
            var surf = noteMap != null ? TermNormalizer.Normalize(NoteFieldMapHelper.GetWord(noteMap)) : string.Empty;
            return (projectTerm != null && c.ProjectTermId == projectTerm.Id)
                   || surf == normalizedSurface;
        }).DistinctBy(c => c.Id).Take(10).ToList();

        var previews = duplicates.Select(card =>
        {
            var progress = card.UserCardProgresses.FirstOrDefault();
            var m = card.Note?.FieldValues ?? new Dictionary<string, NoteFieldValue>();
            var media = NoteFieldMapHelper.BuildCardMedia(m);
            return new CardDuplicatePreviewDto
            {
                Id = card.Id.ToString(),
                NoteId = card.Note?.Id ?? Guid.Empty,
                NoteTypeId = card.Note?.NoteTypeId ?? Guid.Empty,
                FieldValues = new Dictionary<string, NoteFieldValue>(m),
                ProjectTermId = card.ProjectTermId?.ToString("D"),
                SrsStatus = progress != null ? MapProgressState(progress) : "NEW",
                HasAudio = media?.AudioId.HasValue == true || !string.IsNullOrEmpty(media?.AudioUrl),
                DeckTitle = card.Deck.Title
            };
        }).ToList();

        return new CheckCardDuplicatesResponseDto
        {
            IsDuplicate = previews.Count > 0,
            NormalizedSurface = normalizedSurface,
            ExistingCards = previews
        };
    }

    public async Task<Card> CaptureCardAsync(CaptureCardDto dto, CancellationToken cancellationToken = default)
    {
        Deck targetDeck;

        if (dto.DeckId.HasValue)
        {
            targetDeck = await _context.Decks
                    .FirstOrDefaultAsync(
                        d => d.Id == dto.DeckId.Value && d.ProjectId == dto.ProjectId && d.OwnerId == dto.UserId,
                        cancellationToken)
                ?? throw new ArgumentException("Deck not found or does not belong to this project and user.");
        }
        else
        {
            var inboxDeck = await _context.Decks
                .FirstOrDefaultAsync(d => d.ProjectId == dto.ProjectId && d.Title == "Inbox" && d.OwnerId == dto.UserId, cancellationToken);

            if (inboxDeck != null)
            {
                targetDeck = inboxDeck;
            }
            else
            {
                _logger.LogInformation("Creating 'Inbox' deck for project {ProjectId}", dto.ProjectId);
                targetDeck = new Deck
                {
                    Id = Guid.NewGuid(),
                    ProjectId = dto.ProjectId,
                    OwnerId = dto.UserId,
                    Title = "Inbox",
                    Description = "Automatically captured cards",
                    IsPublic = false,
                    ContributionPolicy = "OPEN",
                    LicenseType = "PRIVATE",
                    CardCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Decks.Add(targetDeck);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        Guid? imageId = null;
        if (!string.IsNullOrEmpty(dto.ScreenshotBase64))
        {
            _logger.LogInformation("Card capture includes screenshot (size: {Size} chars)", dto.ScreenshotBase64.Length);
            var (data, contentType) = DecodeBase64Image(dto.ScreenshotBase64);
            if (data != null && data.Length > 0)
            {
                await using var stream = new MemoryStream(data);
                imageId = await _mediaService.UploadImageAsync(stream, contentType, cancellationToken).ConfigureAwait(false);
            }
        }

        var map = NoteFieldMapHelper.NormalizeSentenceMiningMap(dto.FieldValues);
        if (imageId.HasValue)
            map[SentenceMiningNoteType.Image] = new NoteFieldValue { String = imageId.Value.ToString() };

        var expression = TermNormalizer.Normalize(NoteFieldMapHelper.GetExpression(map));
        var word = TermNormalizer.Normalize(NoteFieldMapHelper.GetWord(map));

        if (!string.IsNullOrWhiteSpace(expression) && !string.IsNullOrWhiteSpace(word))
        {
            var projectTerm = await _context.ProjectTerms.AsNoTracking()
                .FirstOrDefaultAsync(t => t.ProjectId == dto.ProjectId && t.NormalizedText == word && t.Type == "WORD", cancellationToken);

            var candidateCardsQuery = _context.Cards
                .Include(c => c.Deck)
                .Include(c => c.Note)
                .Where(c => c.CreatorId == dto.UserId && c.Deck.ProjectId == dto.ProjectId);

            if (projectTerm != null)
            {
                candidateCardsQuery = candidateCardsQuery.Where(c => c.ProjectTermId == projectTerm.Id);
            }
            else
            {
                // Fallback for cards that might not have a ProjectTerm yet. 
                // We order by latest to find recent captures.
                candidateCardsQuery = candidateCardsQuery.OrderByDescending(c => c.UpdatedAt).Take(100);
            }

            var candidateCards = await candidateCardsQuery.ToListAsync(cancellationToken);

            var duplicate = candidateCards.FirstOrDefault(c =>
            {
                var noteMap = c.Note?.FieldValues;
                if (noteMap == null) return false;
                var cExpr = TermNormalizer.Normalize(NoteFieldMapHelper.GetExpression(noteMap));
                var cWord = TermNormalizer.Normalize(NoteFieldMapHelper.GetWord(noteMap));
                return cExpr == expression && cWord == word;
            });

            if (duplicate != null)
            {
                _logger.LogInformation("Exact duplicate found (Card {CardId}) during capture. Updating it instead of creating new.", duplicate.Id);
                var updateDto = new UpdateCardDto { FieldValues = map };
                return await UpdateCardAsync(duplicate.Id, dto.UserId, updateDto, cancellationToken);
            }
        }

        var createDto = new CreateCardDto
        {
            UserId = dto.UserId,
            DeckId = targetDeck.Id,
            FieldValues = map,
        };

        return await CreateCardAsync(createDto, cancellationToken);
    }

    public async Task<Card?> GetCardByIdAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default)
    {
        var card = await _context.Cards
            .Include(c => c.Deck)
            .Include(c => c.Note)
            .Include(c => c.CardTemplate)
            .Include(c => c.UserCardProgresses.Where(p => p.UserId == userId))
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null) return null;

        if (card.CreatorId != userId && card.Deck.OwnerId != userId)
        {
            var isSubscriber = await _context.DeckSubscriptions
                .AnyAsync(s => s.UserId == userId && s.DeckId == card.DeckId, cancellationToken);

            if (!isSubscriber && !card.Deck.IsPublic)
                throw new UnauthorizedAccessException("Access denied to this card");
        }

        await FillResolvedCardMediaAsync(card, cancellationToken).ConfigureAwait(false);
        return card;
    }

    public async Task<Card> UpdateCardAsync(Guid cardId, Guid userId, UpdateCardDto dto, CancellationToken cancellationToken = default)
    {
        var card = await _context.Cards
            .Include(c => c.Deck)
            .Include(c => c.Note)
            .Include(c => c.UserCardProgresses.Where(p => p.UserId == userId))
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null)
            throw new KeyNotFoundException($"Card {cardId} not found");

        if (card.CreatorId != userId && card.Deck.OwnerId != userId)
            throw new UnauthorizedAccessException("No permission to update this card");

        if (dto.FieldValues == null || dto.FieldValues.Count == 0)
        {
            card.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Entry(card).Reference(c => c.Note).LoadAsync(cancellationToken);
            await _context.Entry(card).Reference(c => c.CardTemplate).LoadAsync(cancellationToken);
            await FillResolvedCardMediaAsync(card, cancellationToken).ConfigureAwait(false);
            return card;
        }

        var note = card.Note ?? await _context.Notes.FirstOrDefaultAsync(n => n.Id == card.NoteId, cancellationToken);
        if (note == null)
            throw new InvalidOperationException("Card has no note.");

        NoteFieldMapHelper.MergeInto(note, dto.FieldValues);
        var map = NoteFieldMapHelper.NormalizeSentenceMiningMap(note.FieldValues);
        note.FieldValues = map;

        var expr = NoteFieldMapHelper.GetExpression(map);
        var word = NoteFieldMapHelper.GetWord(map);
        NoteFieldMapHelper.CalculateTargetIndex(expr, word);

        card.SearchDocument = NoteFieldMapHelper.BuildSearchDocument(map);
        // Note-centric updates: clear legacy lemma link; term link refreshed below.
        await _termService.LinkCardWordTermAsync(userId, card, cancellationToken);

        note.ProjectTermId = card.ProjectTermId;

        card.UpdatedAt = DateTime.UtcNow;
        note.UpdatedAt = card.UpdatedAt;

        await _context.SaveChangesAsync(cancellationToken);

        await _context.Entry(card).Reference(c => c.Note).LoadAsync(cancellationToken);
        await _context.Entry(card).Reference(c => c.CardTemplate).LoadAsync(cancellationToken);

        await FillResolvedCardMediaAsync(card, cancellationToken).ConfigureAwait(false);
        return card;
    }

    public async Task DeleteCardAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default)
    {
        var card = await _context.Cards
            .Include(c => c.Deck)
            .FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

        if (card == null) return;

        if (card.CreatorId != userId && card.Deck.OwnerId != userId)
            throw new UnauthorizedAccessException("No permission to delete this card");

        var noteId = card.NoteId;

        _context.Cards.Remove(card);

        if (card.Deck != null)
            card.Deck.CardCount = Math.Max(0, card.Deck.CardCount - 1);

        var note = await _context.Notes.FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);
        if (note != null)
            _context.Notes.Remove(note);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<Card> Items, int TotalCount)> SearchCardsAsync(
        Guid userId,
        string query,
        Guid? projectId,
        Guid? deckId,
        int pageNumber,
        int pageSize,
        List<string>? srsStatuses = null,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Cards
            .Include(c => c.Deck)
            .Include(c => c.Note)
            .Include(c => c.UserCardProgresses.Where(p => p.UserId == userId))
            .Where(c => c.CreatorId == userId);

        if (projectId.HasValue)
            dbQuery = dbQuery.Where(c => c.Deck.ProjectId == projectId.Value);

        if (deckId.HasValue)
            dbQuery = dbQuery.Where(c => c.DeckId == deckId.Value);

        if (srsStatuses != null && srsStatuses.Any())
        {
            var u = srsStatuses.Select(s => s.ToUpperInvariant()).ToList();
            var hasNew = u.Contains("NEW");
            var hasLearning = u.Contains("LEARNING");
            var hasReview = u.Contains("REVIEW");
            var hasRelearning = u.Contains("RELEARNING");
            var hasMature = u.Contains("MATURE");
            var matureDueMin = DateTime.UtcNow.AddDays(21);

            if (hasNew || hasLearning || hasReview || hasRelearning || hasMature)
            {
                dbQuery = from card in dbQuery
                    join progress in _context.UserCardProgresses
                        on card.Id equals progress.CardId into progressJoin
                    from p in progressJoin.DefaultIfEmpty()
                    where
                        (hasNew && (p == null || p.State == 0))
                        || (hasLearning && p != null && p.State == 1)
                        || (hasRelearning && p != null && p.State == 3)
                        || (p != null && p.State == 2
                            && (hasReview || (hasMature && p.Due >= matureDueMin)))
                    select card;
            }
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            dbQuery = dbQuery.Where(c => c.SearchDocument.Contains(q));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        foreach (var card in items)
            await FillResolvedCardMediaAsync(card, cancellationToken).ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<List<Card>> BulkCreateCardsAsync(Guid userId, Guid deckId, List<CreateCardDto> dtos, CancellationToken cancellationToken = default)
    {
        var deck = await _context.Decks.FirstOrDefaultAsync(d => d.Id == deckId && d.OwnerId == userId, cancellationToken);
        if (deck == null)
            throw new UnauthorizedAccessException("Deck not found or access denied");

        var (noteType, defaultTemplate) =
            await _noteTypeService.EnsureSentenceMiningAsync(deck.ProjectId, cancellationToken).ConfigureAwait(false);

        var cards = new List<Card>();
        var notes = new List<Note>();
        foreach (var dto in dtos)
        {
            var cardId = Guid.NewGuid();
            var noteId = Guid.NewGuid();
            var ts = DateTime.UtcNow;
            var map = NoteFieldMapHelper.NormalizeSentenceMiningMap(dto.FieldValues);
            var expr = NoteFieldMapHelper.GetExpression(map);
            var word = NoteFieldMapHelper.GetWord(map);
            NoteFieldMapHelper.CalculateTargetIndex(expr, word);

            var note = new Note
            {
                Id = noteId,
                DeckId = deckId,
                CreatorId = userId,
                NoteTypeId = noteType.Id,
                FieldValues = map,
                CreatedAt = ts,
                UpdatedAt = ts,
            };
            notes.Add(note);

            var searchDoc = NoteFieldMapHelper.BuildSearchDocument(map);
            cards.Add(new Card
            {
                Id = cardId,
                DeckId = deckId,
                CreatorId = userId,
                NoteId = noteId,
                SearchDocument = searchDoc,
                CardTemplateId = defaultTemplate.Id,
                CreatedAt = ts,
                UpdatedAt = ts
            });
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _context.Notes.AddRange(notes);

                _context.Cards.AddRange(cards);
                foreach (var card in cards)
                    await _termService.LinkCardWordTermAsync(userId, card, cancellationToken);

                for (var i = 0; i < cards.Count; i++)
                    notes[i].ProjectTermId = cards[i].ProjectTermId;

                deck.CardCount += cards.Count;
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error during bulk card creation");
                throw;
            }
        });

        _logger.LogInformation("Bulk created {Count} cards in deck {DeckId} for user {UserId}", cards.Count, deckId, userId);
        return cards;
    }

    public async Task SuspendCardAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default)
    {
        var progress = await _context.UserCardProgresses.FirstOrDefaultAsync(p => p.CardId == cardId && p.UserId == userId, cancellationToken);

        if (progress == null)
        {
            var card = await GetCardByIdAsync(cardId, userId, cancellationToken);
            if (card == null) throw new KeyNotFoundException("Card not found");

            progress = new UserCardProgress
            {
                Id = Guid.NewGuid(),
                CardId = cardId,
                UserId = userId,
                ProjectId = card.Deck.ProjectId,
                State = 0,
                Due = DateTime.UtcNow,
                IsSuspended = true,
                LastReview = DateTime.UtcNow
            };
            _context.UserCardProgresses.Add(progress);
        }
        else
        {
            progress.IsSuspended = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Card {CardId} suspended for user {UserId}", cardId, userId);
    }

    public async Task UnsuspendCardAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default)
    {
        var progress = await _context.UserCardProgresses.FirstOrDefaultAsync(p => p.CardId == cardId && p.UserId == userId, cancellationToken);
        if (progress != null)
        {
            progress.IsSuspended = false;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Card {CardId} unsuspended for user {UserId}", cardId, userId);
        }
    }

    public async Task<List<Card>> GetCardPreviewsAsync(Guid userId, List<Guid> cardIds, CancellationToken cancellationToken = default)
    {
        return await _context.Cards
            .Include(c => c.Deck)
            .Include(c => c.Note)
            .Include(c => c.UserCardProgresses.Where(p => p.UserId == userId))
            .Where(c => cardIds.Contains(c.Id) && (c.CreatorId == userId || c.Deck.IsPublic))
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Card> Items, int TotalCount)> GetCardsByDeckAsync(
        Guid userId,
        Guid deckId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var deck = await _context.Decks
            .AnyAsync(d => d.Id == deckId && (d.OwnerId == userId || d.IsPublic), cancellationToken);

        if (!deck)
        {
            var isSubscribed = await _context.DeckSubscriptions
                .AnyAsync(s => s.UserId == userId && s.DeckId == deckId, cancellationToken);

            if (!isSubscribed)
                throw new UnauthorizedAccessException("Access denied to this deck");
        }

        var dbQuery = _context.Cards
            .Include(c => c.Note)
            .Include(c => c.UserCardProgresses.Where(p => p.UserId == userId))
            .Where(c => c.DeckId == deckId);

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        foreach (var card in items)
            await FillResolvedCardMediaAsync(card, cancellationToken).ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<string> GetSrsStatusAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default)
    {
        var progress = await _context.UserCardProgresses
            .FirstOrDefaultAsync(p => p.CardId == cardId && p.UserId == userId, cancellationToken);

        if (progress == null) return "NEW";

        return progress.State switch
        {
            0 => "NEW",
            1 => "LEARNING",
            2 => "REVIEW",
            3 => "RELEARNING",
            _ => "NEW"
        };
    }

    public async Task<int> BulkDeleteCardsAsync(Guid userId, IReadOnlyList<Guid> cardIds, CancellationToken cancellationToken = default)
    {
        if (cardIds.Count == 0) return 0;

        var cards = await _context.Cards
            .Include(c => c.Deck)
            .Where(c => cardIds.Contains(c.Id) && (c.CreatorId == userId || c.Deck.OwnerId == userId))
            .ToListAsync(cancellationToken);

        var noteIds = cards.Select(c => c.NoteId).ToHashSet();
        var notes = await _context.Notes
            .Where(n => noteIds.Contains(n.Id))
            .ToListAsync(cancellationToken);

        var deckCounts = cards.GroupBy(c => c.DeckId)
            .ToDictionary(g => g.Key, g => g.Count());

        _context.Cards.RemoveRange(cards);
        _context.Notes.RemoveRange(notes);

        foreach (var deck in cards.Select(c => c.Deck).Distinct())
        {
            if (deck != null)
                deck.CardCount = Math.Max(0, deck.CardCount - deckCounts.GetValueOrDefault(deck.Id));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return cards.Count;
    }

    public async Task<int> MoveCardsAsync(Guid userId, IReadOnlyList<Guid> cardIds, Guid deckId, CancellationToken cancellationToken = default)
    {
        if (cardIds.Count == 0) return 0;

        var targetDeck = await _context.Decks
            .FirstOrDefaultAsync(d => d.Id == deckId && d.OwnerId == userId, cancellationToken);

        if (targetDeck == null)
            throw new UnauthorizedAccessException("Target deck not found or access denied");

        var cards = await _context.Cards
            .Include(c => c.Deck)
            .Where(c => cardIds.Contains(c.Id) && (c.CreatorId == userId || c.Deck.OwnerId == userId))
            .ToListAsync(cancellationToken);

        var notes = await _context.Notes
            .Where(n => cards.Select(c => c.NoteId).Contains(n.Id))
            .ToListAsync(cancellationToken);

        var sourceCounts = cards.GroupBy(c => c.DeckId)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var card in cards)
        {
            if (card.DeckId != deckId)
            {
                card.DeckId = deckId;
                card.UpdatedAt = DateTime.UtcNow;
            }
        }

        foreach (var note in notes)
        {
            note.DeckId = deckId;
            note.UpdatedAt = DateTime.UtcNow;
        }

        targetDeck.CardCount += cards.Count(c => c.DeckId == deckId);
        foreach (var sourceGroup in cards.Where(c => c.DeckId != deckId).GroupBy(c => c.DeckId))
        {
            var sourceDeck = sourceGroup.First().Deck;
            if (sourceDeck != null)
                sourceDeck.CardCount = Math.Max(0, sourceDeck.CardCount - sourceGroup.Count());
        }

        await _context.SaveChangesAsync(cancellationToken);
        return cards.Count;
    }

    public async Task<int> ResetCardProgressAsync(Guid userId, IReadOnlyList<Guid> cardIds, CancellationToken cancellationToken = default)
    {
        if (cardIds.Count == 0) return 0;

        var progresses = await _context.UserCardProgresses
            .Where(p => cardIds.Contains(p.CardId) && p.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.UserCardProgresses.RemoveRange(progresses);
        await _context.SaveChangesAsync(cancellationToken);
        return progresses.Count;
    }

    public async Task<(List<Card> Items, int TotalCount)> GetLeechCardsAsync(
        Guid userId, Guid projectId, int threshold, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Cards
            .Include(c => c.Deck)
            .Include(c => c.Note)
            .Include(c => c.UserCardProgresses.Where(p => p.UserId == userId))
            .Where(c => c.CreatorId == userId && c.Deck.ProjectId == projectId)
            .Where(c => c.UserCardProgresses.Any(p => p.UserId == userId && p.Lapses >= threshold));

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(c => c.UserCardProgresses.FirstOrDefault(p => p.UserId == userId)!.Lapses)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        foreach (var card in items)
            await FillResolvedCardMediaAsync(card, cancellationToken).ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task<(List<Card> Items, int TotalCount)> GetCardsMissingMediaAsync(
        Guid userId, Guid projectId, string? mediaType, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var candidates = await _context.Cards
            .Include(c => c.Deck)
            .Include(c => c.Note)
            .Where(c => c.CreatorId == userId && c.Deck.ProjectId == projectId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var filtered = mediaType?.ToLowerInvariant() switch
        {
            "image" => candidates.Where(c => c.Note != null && !NoteFieldMapHelper.HasImage(c.Note.FieldValues)).ToList(),
            "audio" => candidates.Where(c => c.Note != null && !NoteFieldMapHelper.HasAudio(c.Note.FieldValues)).ToList(),
            _ => candidates.Where(c => c.Note != null &&
                (!NoteFieldMapHelper.HasImage(c.Note.FieldValues) || !NoteFieldMapHelper.HasAudio(c.Note.FieldValues))).ToList()
        };

        var totalCount = filtered.Count;
        var items = filtered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        foreach (var card in items)
            await FillResolvedCardMediaAsync(card, cancellationToken).ConfigureAwait(false);

        return (items, totalCount);
    }

    private async Task FillResolvedCardMediaAsync(Card card, CancellationToken cancellationToken)
    {
        if (card.Note == null)
            await _context.Entry(card).Reference(c => c.Note).LoadAsync(cancellationToken);
        var media = card.Note != null
            ? NoteFieldMapHelper.BuildCardMedia(card.Note.FieldValues)
            : null;
        await _mediaService.FillCardMediaUrlsAsync(media, cancellationToken).ConfigureAwait(false);
    }

    private static string MapProgressState(UserCardProgress progress)
    {
        return progress.State switch
        {
            0 => "NEW",
            1 => "LEARNING",
            2 when progress.Due >= DateTime.UtcNow.AddDays(21) => "MATURE",
            2 => "REVIEW",
            3 => "RELEARNING",
            _ => "NEW"
        };
    }

    private static (byte[]? data, string contentType) DecodeBase64Image(string screenshotBase64)
    {
        var s = screenshotBase64.Trim();
        var contentType = "image/png";
        if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = s.IndexOf(',');
            if (comma > 0)
            {
                var header = s[5..comma].TrimEnd();
                var mimePart = header.Split(';')[0].Trim();
                if (!string.IsNullOrEmpty(mimePart))
                    contentType = mimePart;
                s = s[(comma + 1)..];
            }
        }

        try
        {
            var bytes = Convert.FromBase64String(s);
            return (bytes, contentType);
        }
        catch (FormatException)
        {
            return (null, contentType);
        }
    }
}
