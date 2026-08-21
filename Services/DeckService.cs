using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Dtos;
using VocabularyService.Options;

namespace VocabularyService.Services;

/// <summary>
/// Сервис для работы с колодами
/// </summary>
public class DeckService : IDeckService
{
    private readonly VocabularyServiceContext _context;
    private readonly VocabularyServiceOptions _options;
    private readonly ILogger<DeckService> _logger;
    private readonly IProjectService _projectService;
    private readonly ICommunityService? _communityService;
    private const int MaxDeckDepth = 10;
    private const int LearnAheadLimitMinutes = 20;

    private sealed class CardProgressSnapshot
    {
        public Guid DeckId { get; init; }
        public int Reps { get; init; }
        public int State { get; init; }
        public DateTime Due { get; init; }
        public int ScheduledDays { get; init; }
        public bool IsSuspended { get; init; }
        public int Lapses { get; init; }
    }

    public DeckService(
        VocabularyServiceContext context,
        IOptions<VocabularyServiceOptions> options,
        ILogger<DeckService> logger,
        IProjectService projectService,
        ICommunityService? communityService = null)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
        _projectService = projectService;
        _communityService = communityService;
    }

    /// <summary>
    /// Получает дерево колод для проекта
    /// </summary>
    public async Task<List<DeckTreeItem>> GetDeckTreeAsync(
        Guid projectId,
        Guid userId,
        IDeckService.LibraryFilterKind libraryFilter = IDeckService.LibraryFilterKind.Unspecified,
        CancellationToken cancellationToken = default)
    {
        // Проверка доступа к проекту
        var project = await _projectService.GetProjectByIdAsync(projectId, userId, cancellationToken);
        if (project == null)
        {
            throw new UnauthorizedAccessException("Project not found or access denied");
        }

        // Загружаем все колоды проекта
        var decks = await _context.Decks
            .Where(d => d.ProjectId == projectId)
            .OrderBy(d => d.Title)
            .ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);

        // Применяем фильтр библиотеки (Мои / Скачанные / Публичные)
        if (libraryFilter != IDeckService.LibraryFilterKind.Unspecified)
        {
            decks = libraryFilter switch
            {
                IDeckService.LibraryFilterKind.Mine => decks.Where(d => d.OwnerId == userId).ToList(),
                IDeckService.LibraryFilterKind.Downloaded => decks.Where(d => d.ForkedFromId != null).ToList(),
                IDeckService.LibraryFilterKind.Public => decks.Where(d => d.IsPublic).ToList(),
                _ => decks
            };
        }

        var deckIds = new HashSet<Guid>(decks.Select(d => d.Id));

        // Группируем по ParentDeckId для быстрого доступа (только колоды из отфильтрованного набора)
        var decksByParent = new Dictionary<Guid, List<Deck>>();
        List<Deck> rootDecks = new List<Deck>();

        foreach (var deck in decks)
        {
            if (deck.ParentDeckId.HasValue && deckIds.Contains(deck.ParentDeckId.Value))
            {
                var parentId = deck.ParentDeckId.Value;
                if (!decksByParent.TryGetValue(parentId, out var deckList))
                {
                    deckList = new List<Deck>();
                    decksByParent[parentId] = deckList;
                }
                deckList.Add(deck);
            }
            else
            {
                rootDecks.Add(deck);
            }
        }

        // Загружаем статистику по карточкам для всех колод проекта одним запросом
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(LearnAheadLimitMinutes);

        var totalCardsByDeck = await _context.Cards
            .AsNoTracking()
            .Where(c => deckIds.Contains(c.DeckId))
            .GroupBy(c => c.DeckId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);

        var progressList = await _context.UserCardProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.ProjectId == projectId && deckIds.Contains(p.Card.DeckId))
            .Select(p => new CardProgressSnapshot
            {
                DeckId = p.Card.DeckId,
                Reps = p.Reps,
                State = p.State,
                Due = p.Due,
                ScheduledDays = p.ScheduledDays,
                IsSuspended = p.IsSuspended,
                Lapses = p.Lapses
            })
            .ToListAsync(cancellationToken);

        var progressByDeck = progressList
            .GroupBy(p => p.DeckId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Строим дерево рекурсивно, начиная с корневых узлов
        return rootDecks.Select(deck => BuildTreeItem(deck, decksByParent, totalCardsByDeck, progressByDeck, now, cutoff)).ToList();
    }

    /// <summary>
    /// Создает новую колоду
    /// </summary>
    public async Task<Deck> CreateDeckAsync(
        CreateDeckDto dto,
        CancellationToken cancellationToken = default)
    {
        // Валидация названия
        if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 3 || dto.Title.Length > 100)
        {
            throw new ArgumentException("Title must be between 3 and 100 characters", nameof(dto));
        }

        // Проверка существования проекта
        var project = await _projectService.GetProjectByIdAsync(dto.ProjectId, dto.UserId, cancellationToken);
        if (project == null)
        {
            throw new UnauthorizedAccessException("Project not found or access denied");
        }

        // Если указан родитель, проверяем его
        if (dto.ParentDeckId.HasValue)
        {
            var parentDeck = await _context.Decks
                .FirstOrDefaultAsync(d => d.Id == dto.ParentDeckId.Value, cancellationToken);

            if (parentDeck == null)
            {
                throw new ArgumentException("Parent deck not found", nameof(dto));
            }

            // Проверка принадлежности к тому же проекту
            if (parentDeck.ProjectId != dto.ProjectId)
            {
                throw new InvalidOperationException("Parent deck belongs to a different project");
            }

            // Проверка глубины вложенности
            var depth = await GetDeckDepthAsync(parentDeck.Id, cancellationToken);
            if (depth >= MaxDeckDepth)
            {
                throw new InvalidOperationException($"Maximum deck depth ({MaxDeckDepth}) exceeded");
            }
        }

        // Создаем колоду
        var deck = new Deck
        {
            ProjectId = dto.ProjectId,
            ParentDeckId = dto.ParentDeckId,
            OwnerId = dto.UserId,
            Title = dto.Title,
            Description = dto.Description,
            CoverImageUrl = dto.CoverImageUrl,
            IsPublic = dto.IsPublic,
            ContributionPolicy = "CLOSED",
            LicenseType = "PRIVATE",
            ForkedFromId = null,
            CardCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Decks.Add(deck);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created deck {DeckId} with title '{Title}' for project {ProjectId}",
            deck.Id,
            dto.Title,
            dto.ProjectId);

        return deck;
    }

    /// <summary>
    /// Обновляет колоду
    /// </summary>
    public async Task<Deck> UpdateDeckAsync(
        Guid deckId,
        Guid userId,
        UpdateDeckDto dto,
        CancellationToken cancellationToken = default)
    {
        var deck = await GetDeckByIdAsync(deckId, userId, cancellationToken);
        if (deck == null)
        {
            throw new UnauthorizedAccessException("Deck not found or access denied");
        }

        // Проверка прав (пользователь должен быть владельцем)
        if (deck.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("User is not the owner of the deck");
        }

        // Обновление названия
        if (dto.Title != null)
        {
            if (dto.Title.Length < 3 || dto.Title.Length > 100)
            {
                throw new ArgumentException("Title must be between 3 and 100 characters", nameof(dto));
            }
            deck.Title = dto.Title;
        }

        // Обновление описания
        if (dto.Description != null)
        {
            deck.Description = dto.Description;
        }

        // Обновление обложки
        if (dto.CoverImageUrl != null)
        {
            deck.CoverImageUrl = dto.CoverImageUrl;
        }

        // Обновление публичности
        if (dto.IsPublic.HasValue)
        {
            // Защита контента: если публикуем колоду с COMMERCIAL_DERIVATIVE лицензией
            if (dto.IsPublic.Value && deck.LicenseType == "COMMERCIAL_DERIVATIVE")
            {
                throw new InvalidOperationException("Cannot publish deck with COMMERCIAL_DERIVATIVE license");
            }
            deck.IsPublic = dto.IsPublic.Value;
        }

        // Обновление политики вкладов
        if (dto.ContributionPolicy != null)
        {
            deck.ContributionPolicy = dto.ContributionPolicy;
        }

        // Обновление типа лицензии
        if (dto.LicenseType != null)
        {
            deck.LicenseType = dto.LicenseType;
        }

        // Обновление родительской колоды (перемещение)
        if (dto.ParentDeckId != deck.ParentDeckId)
        {
            await ValidateDeckMoveAsync(deck, dto.ParentDeckId, cancellationToken);
            deck.ParentDeckId = dto.ParentDeckId;
        }

        deck.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated deck {DeckId} by user {UserId}",
            deckId,
            userId);

        return deck;
    }

    /// <summary>
    /// Удаляет колоду
    /// </summary>
    public async Task DeleteDeckAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var deck = await GetDeckByIdAsync(deckId, userId, cancellationToken);
        if (deck == null)
        {
            throw new UnauthorizedAccessException("Deck not found or access denied");
        }

        // Проверка прав
        if (deck.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("User is not the owner of the deck");
        }

        // Automatic Detachment (SR-COL-08): создать Fork для активных пользователей
        if (deck.IsPublic && _communityService != null)
        {
            try
            {
                var detachedCount = await _communityService.DetachActiveUsersAsync(
                    deckId, userId, cancellationToken);

                _logger.LogInformation(
                    "Automatic Detachment completed for deck {DeckId}: {Count} users detached",
                    deckId, detachedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error during Automatic Detachment for deck {DeckId}. Proceeding with deletion.",
                    deckId);
                // Продолжаем удаление даже если detachment не удался
            }
        }

        // Создаем запись в deleted_objects (Tombstone) для синхронизации
        var deletedObject = new DeletedObject
        {
            Id = Guid.NewGuid(),
            EntityId = deckId,
            EntityType = "Deck",
            UserId = userId,
            ParentId = deck.ProjectId,
            DeletedAt = DateTime.UtcNow
        };

        _context.DeletedObjects.Add(deletedObject);

        // Удаляем колоду (каскадное удаление карточек и под-колод через ON DELETE CASCADE)
        _context.Decks.Remove(deck);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted deck {DeckId} by user {UserId}",
            deckId,
            userId);
    }

    /// <summary>
    /// Получает колоду по идентификатору
    /// </summary>
    public async Task<Deck?> GetDeckByIdAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var deck = await _context.Decks
            .FirstOrDefaultAsync(d => d.Id == deckId, cancellationToken);

        if (deck == null)
        {
            return null;
        }

        // Проверка доступа через проверку проекта
        var project = await _projectService.GetProjectByIdAsync(deck.ProjectId, userId, cancellationToken);
        if (project == null)
        {
            return null;
        }

        return deck;
    }

    /// <summary>
    /// Получает детальную информацию о колоде со статистикой карточек для пользователя
    /// </summary>
    public async Task<DeckDetailDto?> GetDeckDetailAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var deck = await _context.Decks
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deckId, cancellationToken);

        if (deck == null)
        {
            return null;
        }

        var project = await _projectService.GetProjectByIdAsync(deck.ProjectId, userId, cancellationToken);
        if (project == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(LearnAheadLimitMinutes);
        var totalCards = await _context.Cards
            .AsNoTracking()
            .CountAsync(c => c.DeckId == deckId, cancellationToken);

        // Прогресс пользователя по карточкам этой колоды (в контексте проекта)
        var progressList = await _context.UserCardProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.ProjectId == deck.ProjectId && p.Card.DeckId == deckId)
            .Select(p => new { p.Reps, p.State, p.Due, p.ScheduledDays, p.IsSuspended, p.Lapses })
            .ToListAsync(cancellationToken);

        var activeProgress = progressList.Where(p => !p.IsSuspended).ToList();

        // New: карточки без прогресса или с Repetitions == 0
        var newCount = (totalCards - progressList.Count) + activeProgress.Count(p => p.Reps == 0);

        // Learning: intraday cards due within learn-ahead window (matches Study queue learn-ahead)
        var learningCount = activeProgress.Count(p =>
            (p.State == 1 || p.State == 3 || p.ScheduledDays < 1) && p.Due <= cutoff);

        // To review: mature review cards due now (State == REVIEW)
        var dueCount = activeProgress.Count(p => p.State == 2 && p.Due <= now);

        // Study Now: cards the session queue can actually surface today
        var studyableNewWithoutProgress = totalCards - progressList.Count;
        var studyableUnreviewedWithProgress = activeProgress.Count(p =>
            p.State == 0 && p.Reps == 0 && p.Lapses == 0);
        var studyableDueLearningReview = activeProgress.Count(p =>
            (p.State == 2 && p.Due <= now)
            || ((p.State == 1 || p.State == 3) && p.Due <= cutoff)
            || (p.State == 0 && p.Lapses > 0 && p.Due <= now));
        var studyableNowCount = studyableNewWithoutProgress
            + studyableUnreviewedWithProgress
            + studyableDueLearningReview;

        return new DeckDetailDto
        {
            Id = deck.Id,
            Title = deck.Title,
            Description = deck.Description,
            ParentDeckId = deck.ParentDeckId,
            ProjectId = deck.ProjectId,
            OwnerId = deck.OwnerId,
            CoverImageUrl = deck.CoverImageUrl,
            IsPublic = deck.IsPublic,
            ContributionPolicy = deck.ContributionPolicy,
            LicenseType = deck.LicenseType,
            ForkedFromId = deck.ForkedFromId,
            CreatedAt = deck.CreatedAt,
            CardCount = deck.CardCount,
            Stats = new DeckDetailStatsDto
            {
                NewCardsCount = newCount,
                LearningCardsCount = learningCount,
                DueCardsCount = dueCount,
                StudyableNowCount = studyableNowCount,
                TotalCardsCount = totalCards
            }
        };
    }

    /// <summary>
    /// Строит узел дерева рекурсивно и вычисляет статистику карточек для пользователя.
    /// </summary>
    private DeckTreeItem BuildTreeItem(
        Deck deck,
        Dictionary<Guid, List<Deck>> decksByParent,
        Dictionary<Guid, int> totalCardsByDeck,
        Dictionary<Guid, List<CardProgressSnapshot>> progressByDeck,
        DateTime now,
        DateTime cutoff)
    {
        var item = new DeckTreeItem
        {
            Id = deck.Id,
            Title = deck.Title,
            CardCount = deck.CardCount,
            OwnerId = deck.OwnerId,
            IsPublic = deck.IsPublic,
            ForkedFromId = deck.ForkedFromId,
            CoverImageUrl = deck.CoverImageUrl,
            Stats = CalculateDeckStats(deck.Id, totalCardsByDeck, progressByDeck, now, cutoff)
        };

        // Рекурсивно строим дочерние узлы
        if (decksByParent.TryGetValue(deck.Id, out var children))
        {
            item.Children = children
                .Select(child => BuildTreeItem(child, decksByParent, totalCardsByDeck, progressByDeck, now, cutoff))
                .ToList();
        }

        return item;
    }

    /// <summary>
    /// Вычисляет статистику карточек для одной колоды.
    /// </summary>
    private DeckDetailStatsDto CalculateDeckStats(
        Guid deckId,
        Dictionary<Guid, int> totalCardsByDeck,
        Dictionary<Guid, List<CardProgressSnapshot>> progressByDeck,
        DateTime now,
        DateTime cutoff)
    {
        var totalCards = totalCardsByDeck.GetValueOrDefault(deckId);
        var progressList = progressByDeck.GetValueOrDefault(deckId) ?? new List<CardProgressSnapshot>();
        var activeProgress = progressList.Where(p => !p.IsSuspended).ToList();

        var newCount = (totalCards - progressList.Count) + activeProgress.Count(p => p.Reps == 0);

        var learningCount = activeProgress.Count(p =>
            (p.State == 1 || p.State == 3 || p.ScheduledDays < 1) && p.Due <= cutoff);

        var dueCount = activeProgress.Count(p => p.State == 2 && p.Due <= now);

        var studyableNewWithoutProgress = totalCards - progressList.Count;
        var studyableUnreviewedWithProgress = activeProgress.Count(p =>
            p.State == 0 && p.Reps == 0 && p.Lapses == 0);
        var studyableDueLearningReview = activeProgress.Count(p =>
            (p.State == 2 && p.Due <= now)
            || ((p.State == 1 || p.State == 3) && p.Due <= cutoff)
            || (p.State == 0 && p.Lapses > 0 && p.Due <= now));
        var studyableNowCount = studyableNewWithoutProgress
            + studyableUnreviewedWithProgress
            + studyableDueLearningReview;

        return new DeckDetailStatsDto
        {
            NewCardsCount = newCount,
            LearningCardsCount = learningCount,
            DueCardsCount = dueCount,
            StudyableNowCount = studyableNowCount,
            TotalCardsCount = totalCards
        };
    }

    /// <summary>
    /// Получает глубину колоды в иерархии
    /// </summary>
    private async Task<int> GetDeckDepthAsync(Guid deckId, CancellationToken cancellationToken)
    {
        int depth = 0;
        Guid? currentDeckId = deckId;

        while (currentDeckId.HasValue)
        {
            var deck = await _context.Decks
                .FirstOrDefaultAsync(d => d.Id == currentDeckId.Value, cancellationToken);

            if (deck == null || deck.ParentDeckId == null)
            {
                break;
            }

            depth++;
            currentDeckId = deck.ParentDeckId;

            // Защита от бесконечного цикла
            if (depth > MaxDeckDepth)
            {
                break;
            }
        }

        return depth;
    }

    /// <summary>
    /// Валидирует перемещение колоды
    /// </summary>
    private async Task ValidateDeckMoveAsync(Deck deck, Guid? newParentId, CancellationToken cancellationToken)
    {
        // Если перемещаем в корень, проверка не нужна
        if (!newParentId.HasValue)
        {
            return;
        }

        // Проверка существования нового родителя
        var newParent = await _context.Decks
            .FirstOrDefaultAsync(d => d.Id == newParentId.Value, cancellationToken)
            ?? throw new ArgumentException("New parent deck not found");

        // Проверка принадлежности к тому же проекту
        if (newParent.ProjectId != deck.ProjectId)
        {
            throw new InvalidOperationException("New parent deck belongs to a different project");
        }

        // Проверка на циклические ссылки: колода не может стать родителем самой себя
        if (newParentId.Value == deck.Id)
        {
            throw new InvalidOperationException("Deck cannot be its own parent");
        }

        // Проверка на циклические ссылки: колода не может стать родителем своих потомков
        if (await IsDescendantAsync(newParentId.Value, deck.Id, cancellationToken))
        {
            throw new InvalidOperationException("Deck cannot be moved to its own descendant");
        }

        // Проверка глубины вложенности
        var newParentDepth = await GetDeckDepthAsync(newParentId.Value, cancellationToken);
        if (newParentDepth >= MaxDeckDepth)
        {
            throw new InvalidOperationException($"Maximum deck depth ({MaxDeckDepth}) would be exceeded");
        }
    }

    /// <summary>
    /// Проверяет, является ли одна колода потомком другой
    /// </summary>
    private async Task<bool> IsDescendantAsync(Guid potentialDescendantId, Guid ancestorId, CancellationToken cancellationToken)
    {
        Guid? currentId = potentialDescendantId;
        int maxIterations = MaxDeckDepth;

        while (currentId.HasValue && maxIterations > 0)
        {
            var deck = await _context.Decks
                .FirstOrDefaultAsync(d => d.Id == currentId.Value, cancellationToken);

            if (deck == null)
            {
                break;
            }

            if (deck.Id == ancestorId)
            {
                return true;
            }

            currentId = deck.ParentDeckId;
            maxIterations--;
        }

        return false;
    }
}

