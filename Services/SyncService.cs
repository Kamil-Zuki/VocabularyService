using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Dtos.Sync;

namespace VocabularyService.Services;

/// <summary>
/// Сервис для синхронизации данных
/// </summary>
public class SyncService : ISyncService
{
    private readonly VocabularyServiceContext _context;
    private readonly ILogger<SyncService> _logger;
    private readonly IFsrsScheduler _fsrsScheduler;
    private const int MaxSyncDays = 30; // Максимальный возраст токена синхронизации

    public SyncService(
        VocabularyServiceContext context,
        ILogger<SyncService> logger,
        IFsrsScheduler fsrsScheduler)
    {
        _context = context;
        _logger = logger;
        _fsrsScheduler = fsrsScheduler;
    }

    /// <summary>
    /// Получает дельту изменений для синхронизации (SR-SNC-01)
    /// </summary>
    public async Task<SyncDataResponseDto> SyncDataAsync(
        Guid userId,
        SyncDataRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var lastSyncTime = request.LastSyncToken ?? DateTime.MinValue;

        // Проверка на необходимость полной синхронизации
        bool requiresFullSync = false;
        if (request.LastSyncToken == null || 
            (now - lastSyncTime).TotalDays > MaxSyncDays)
        {
            requiresFullSync = true;
            _logger.LogInformation(
                "Full sync required for user {UserId}. Last sync: {LastSync}, Days since: {Days}",
                userId, request.LastSyncToken, (now - lastSyncTime).TotalDays);
        }

        var changes = new SyncChangesDto();

        // Получение измененных колод
        var decksQuery = _context.Decks
            .Where(d => d.OwnerId == userId && d.UpdatedAt > lastSyncTime);

        if (request.ProjectId.HasValue)
        {
            decksQuery = decksQuery.Where(d => d.ProjectId == request.ProjectId.Value);
        }

        var decks = await decksQuery
            .OrderBy(d => d.UpdatedAt)
            .ToListAsync(cancellationToken);

        changes.Decks = decks.Select(d => new SyncDeckDto
        {
            Id = d.Id,
            ProjectId = d.ProjectId,
            ParentDeckId = d.ParentDeckId,
            OwnerId = d.OwnerId,
            Title = d.Title,
            Description = d.Description,
            CoverImageUrl = d.CoverImageUrl,
            IsPublic = d.IsPublic,
            ContributionPolicy = d.ContributionPolicy,
            LicenseType = d.LicenseType,
            ForkedFromId = d.ForkedFromId,
            CardCount = d.CardCount,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();

        // Получение измененных карточек
        var cardsQuery = _context.Cards
            .Include(c => c.Deck)
            .Include(c => c.Note)
            .Where(c => c.Deck.OwnerId == userId && c.UpdatedAt > lastSyncTime);

        if (request.ProjectId.HasValue)
        {
            cardsQuery = cardsQuery.Where(c => c.Deck.ProjectId == request.ProjectId.Value);
        }

        var cards = await cardsQuery
            .OrderBy(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);

        changes.Cards = cards.Select(c => new SyncCardDto
        {
            Id = c.Id,
            DeckId = c.DeckId,
            CreatorId = c.CreatorId,
            NoteId = c.NoteId,
            FieldValues = c.Note != null
                ? new Dictionary<string, NoteFieldValue>(c.Note.FieldValues, StringComparer.Ordinal)
                : new Dictionary<string, NoteFieldValue>(),
            SearchDocument = c.SearchDocument,
            ProjectTermId = c.ProjectTermId,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        // Получение измененного прогресса
        var progressQuery = _context.UserCardProgresses
            .Where(p => p.UserId == userId && p.LastReview > lastSyncTime);

        if (request.ProjectId.HasValue)
        {
            progressQuery = progressQuery.Where(p => p.ProjectId == request.ProjectId.Value);
        }

        var progress = await progressQuery
            .OrderBy(p => p.LastReview)
            .ToListAsync(cancellationToken);

        changes.Progress = progress.Select(p => new SyncProgressDto
        {
            CardId = p.CardId,
            ProjectId = p.ProjectId,
            State = p.State,
            Stability = p.Stability,
            Difficulty = p.Difficulty,
            Due = p.Due,
            ElapsedDays = p.ElapsedDays,
            ScheduledDays = p.ScheduledDays,
            Reps = p.Reps,
            Lapses = p.Lapses,
            IsSuspended = p.IsSuspended,
            LastReview = p.LastReview
        }).ToList();

        // Получение удаленных объектов (Tombstones)
        var deletedObjectsQuery = _context.DeletedObjects
            .Where(d => d.UserId == userId && d.DeletedAt > lastSyncTime);

        if (request.ProjectId.HasValue)
        {
            // Для deleted_objects нужно проверить parent_id, если это проект
            // Но обычно deleted_objects не имеет прямой связи с project_id
            // Поэтому фильтруем только по user_id и deleted_at
        }

        var deletedObjects = await deletedObjectsQuery
            .OrderBy(d => d.DeletedAt)
            .ToListAsync(cancellationToken);

        var deletedInfos = deletedObjects.Select(d => new DeletedObjectInfoDto
        {
            EntityId = d.EntityId,
            EntityType = d.EntityType
        }).ToList();

        return new SyncDataResponseDto
        {
            SyncToken = now,
            RequiresFullSync = requiresFullSync,
            Changes = changes,
            DeletedObjects = deletedInfos,
            HasMore = false // В будущем можно добавить пагинацию
        };
    }

    /// <summary>
    /// Обрабатывает пакетную отправку офлайн-ответов (SR-SNC-03)
    /// </summary>
    public async Task<BatchSubmitReviewsResponseDto> BatchSubmitReviewsAsync(
        Guid userId,
        BatchSubmitReviewsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Reviews == null || request.Reviews.Count == 0)
        {
            return new BatchSubmitReviewsResponseDto
            {
                ProcessedCount = 0,
                FailedCount = 0,
                FailedCardIds = new List<Guid>()
            };
        }

        // Сортировка по reviewedAt (по возрастанию)
        var sortedReviews = request.Reviews
            .OrderBy(r => r.ReviewedAt)
            .ToList();

        var processedCount = 0;
        var failedCount = 0;
        var failedCardIds = new List<Guid>();

        // Используем транзакцию для атомарности
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var review in sortedReviews)
            {
                try
                {
                    // Загружаем карточку и проверяем, что она существует
                    var card = await _context.Cards
                        .Include(c => c.Deck)
                        .ThenInclude(d => d.Project)
                        .Include(c => c.CardTemplate)
                        .FirstOrDefaultAsync(c => c.Id == review.CardId, cancellationToken);

                    if (card == null)
                    {
                        _logger.LogWarning(
                            "Card {CardId} not found for batch review by user {UserId}",
                            review.CardId, userId);
                        failedCount++;
                        failedCardIds.Add(review.CardId);
                        continue;
                    }

                    // Проверяем, что карточка принадлежит пользователю (через deck)
                    if (card.Deck.OwnerId != userId)
                    {
                        _logger.LogWarning(
                            "Card {CardId} does not belong to user {UserId}",
                            review.CardId, userId);
                        failedCount++;
                        failedCardIds.Add(review.CardId);
                        continue;
                    }

                    var project = card.Deck.Project;

                    // Загружаем или создаем прогресс
                    var progress = await _context.UserCardProgresses
                        .FirstOrDefaultAsync(
                            p => p.UserId == userId && p.CardId == review.CardId && p.ProjectId == project.Id,
                            cancellationToken);

                    if (progress == null)
                    {
                        // Создаем новый прогресс
                        progress = new UserCardProgress
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            CardId = review.CardId,
                            ProjectId = project.Id,
                            State = 0, // NEW
                            Stability = 0,
                            Difficulty = 0,
                            Due = DateTime.UtcNow,
                            ElapsedDays = 0,
                            ScheduledDays = 0,
                            Reps = 0,
                            Lapses = 0,
                            IsSuspended = false,
                            LastReview = review.ReviewedAt
                        };
                        _context.UserCardProgresses.Add(progress);
                    }

                    // Сохраняем состояние ДО ревью для review_log (до любых изменений progress)
                    var stateBefore = progress.State;
                    var lastReviewBefore = progress.LastReview;
                    var dueBefore = progress.Due;
                    var stabilityBefore = progress.Stability;
                    var difficultyBefore = progress.Difficulty;

                    // Применяем FSRS: asOfUtc = review.ReviewedAt для корректного elapsed и due в офлайн-режиме
                    var nextState = await _fsrsScheduler.GetNextStateAsync(
                        progress,
                        review.Rating,
                        review.ReviewedAt,
                        review.DurationMs,
                        project.FsrsSettings,
                        cancellationToken);

                    // Обновляем прогресс
                    progress.State = nextState.State;
                    progress.Step = nextState.Step;
                    progress.Stability = nextState.Stability;
                    progress.Difficulty = nextState.Difficulty;
                    progress.Due = nextState.Due;

                    // Обновляем счетчики
                    if (stateBefore == 0 && nextState.State > 0)
                    {
                        progress.Reps = 1;
                    }
                    else if (stateBefore > 0)
                    {
                        progress.Reps++;
                    }

                    // Anki: lapse count increases on Again only for Review/Relearning cards
                    if (review.Rating == 1 && stateBefore is 2 or 3)
                    {
                        progress.Lapses++;
                    }

                    // Рассчитываем elapsed_days и scheduled_days относительно review.ReviewedAt
                    var elapsed = (review.ReviewedAt - lastReviewBefore).TotalDays;
                    if (elapsed < 0) elapsed = 0;
                    progress.ElapsedDays = (int)elapsed;
                    progress.ScheduledDays = (int)Math.Max(0, (nextState.Due - review.ReviewedAt).TotalDays);
                    progress.LastReview = review.ReviewedAt;

                    // Создаем запись в review_logs (Before — сохранённые до обновления progress значения)
                    var reviewLog = new ReviewLog
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        CardId = review.CardId,
                        SessionId = review.SessionId ?? Guid.Empty, // Для офлайн-режима может быть пустым
                        Rating = (short)review.Rating,
                        StateBefore = stateBefore,
                        StateAfter = nextState.State,
                        DueBefore = dueBefore,
                        DueAfter = nextState.Due,
                        StabilityBefore = stabilityBefore,
                        StabilityAfter = nextState.Stability,
                        DifficultyBefore = difficultyBefore,
                        DifficultyAfter = nextState.Difficulty,
                        ReviewDurationMs = review.DurationMs,
                        CreatedAt = review.ReviewedAt,
                        UserAnswer = review.UserAnswer,
                        AnswerValidationResult = null // Для офлайн-режима валидация не выполняется
                    };

                    _context.ReviewLogs.Add(reviewLog);

                    // Deep Skill Tracking (Phase 2)
                    if (card.ProjectTermId.HasValue)
                    {
                        var termStatus = await _context.UserTermStatuses
                            .FirstOrDefaultAsync(ts => ts.UserId == userId && ts.ProjectTermId == card.ProjectTermId.Value, cancellationToken);
                        
                        if (termStatus != null)
                        {
                            var targetSkill = card.CardTemplate?.TargetSkill ?? "Reading";
                            int newLevel = (int)Math.Min(100, Math.Round(nextState.Stability * 5));

                            switch (targetSkill.ToLowerInvariant())
                            {
                                case "listening":
                                    termStatus.ListeningLevel = Math.Max(termStatus.ListeningLevel, newLevel);
                                    break;
                                case "writing":
                                    termStatus.WritingLevel = Math.Max(termStatus.WritingLevel, newLevel);
                                    break;
                                case "speaking":
                                    termStatus.SpeakingLevel = Math.Max(termStatus.SpeakingLevel, newLevel);
                                    break;
                                default:
                                    termStatus.ReadingLevel = Math.Max(termStatus.ReadingLevel, newLevel);
                                    break;
                            }
                        }
                    }

                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing batch review for card {CardId} by user {UserId}",
                        review.CardId, userId);
                    failedCount++;
                    failedCardIds.Add(review.CardId);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Batch reviews processed for user {UserId}: {Processed} successful, {Failed} failed",
                userId, processedCount, failedCount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error processing batch reviews for user {UserId}", userId);
            throw;
        }

        return new BatchSubmitReviewsResponseDto
        {
            ProcessedCount = processedCount,
            FailedCount = failedCount,
            FailedCardIds = failedCardIds
        };
    }
}
