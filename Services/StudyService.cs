using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Domain;
using VocabularyService.Dtos.Study;
using VocabularyService.Helpers;
using VocabularyService.Services.Study;

namespace VocabularyService.Services;

/// <summary>
/// Сервис для работы с обучением
/// </summary>
public class StudyService : IStudyService
{
    private readonly VocabularyServiceContext _context;
    private readonly ILogger<StudyService> _logger;
    private readonly ICardService _cardService;
    private readonly IDeckService _deckService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IFsrsScheduler _fsrsScheduler;
    private readonly IFsrsPreviewService _fsrsPreviewService;
    private readonly IAnkiStudyQueueService _queueService;
    private readonly IMediaService _mediaService;
    private readonly IDatabase _redis;

    public StudyService(
        VocabularyServiceContext context,
        ILogger<StudyService> logger,
        ICardService cardService,
        IDeckService deckService,
        IUserSettingsService userSettingsService,
        IFsrsScheduler fsrsScheduler,
        IFsrsPreviewService fsrsPreviewService,
        IAnkiStudyQueueService queueService,
        IMediaService mediaService,
        IConnectionMultiplexer redis)
    {
        _context = context;
        _logger = logger;
        _cardService = cardService;
        _deckService = deckService;
        _userSettingsService = userSettingsService;
        _fsrsScheduler = fsrsScheduler;
        _fsrsPreviewService = fsrsPreviewService;
        _queueService = queueService;
        _mediaService = mediaService;
        _redis = redis.GetDatabase();
    }

    private static string GetSeenTermsKey(Guid sessionId) => StudyQueueConstants.SeenTermsKey(sessionId);
    private static string GetSeenTermCardsKey(Guid sessionId) => StudyQueueConstants.SeenTermCardsKey(sessionId);
    private static string GetLegacySeenLemmasKey(Guid sessionId) => StudyQueueConstants.LegacySeenLemmasKey(sessionId);
    private static string GetLegacySeenLemmaCardsKey(Guid sessionId) => StudyQueueConstants.LegacySeenLemmaCardsKey(sessionId);

    /// <summary>
    /// Запускает новую сессию обучения и генерирует очередь карточек (SR-LRN-01)
    /// </summary>
    public async Task<StudySessionDto> StartStudySessionAsync(
        Guid userId,
        Guid projectId,
        Guid? deckId,
        CancellationToken cancellationToken = default)
    {
        // Verify project exists and belongs to user
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, cancellationToken);

        if (project == null)
        {
            throw new KeyNotFoundException($"Project {projectId} not found or access denied");
        }

        // Close any existing active session for this user/project
        List<StudySession> existingSessions;
        try
        {
            existingSessions = await _context.StudySessions
                .Where(s => s.UserId == userId && s.ProjectId == projectId && s.Status == "ACTIVE")
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (IsMissingStudySessionStatusColumn(ex))
        {
            await EnsureStudySessionStatusColumnAsync(cancellationToken);
            existingSessions = await _context.StudySessions
                .Where(s => s.UserId == userId && s.ProjectId == projectId && s.Status == "ACTIVE")
                .ToListAsync(cancellationToken);
        }

        foreach (var existingSession in existingSessions)
        {
            existingSession.Status = "COMPLETED";
            existingSession.EndTime = DateTime.UtcNow;
        }

        // Collect deck IDs (recursively if deckId provided, or all decks in project)
        var deckIds = new List<Guid>();
        if (deckId.HasValue)
        {
            // Recursively collect child decks
            deckIds = await CollectDeckIdsRecursiveAsync(deckId.Value, userId, cancellationToken);
        }
        else
        {
            // Get all decks in project
            deckIds = await _context.Decks
                .Where(d => d.ProjectId == projectId)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);
        }

        if (!deckIds.Any())
        {
            throw new InvalidOperationException("No decks found for study session");
        }

        // Get user settings for daily limits
        var userSettings = await _userSettingsService.GetUserSettingsAsync(userId, cancellationToken);

        // Generate queue with priority: Lapses -> Reviews -> New
        var queue = await GenerateQueueAsync(
            userId,
            projectId,
            deckIds,
            userSettings.DailyGoalNew,
            userSettings.DailyGoalReview,
            cancellationToken);

        // Create session
        var session = new StudySession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = projectId,
            DeckId = deckId,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow, // Will be updated when session ends
            Status = "ACTIVE",
            CardsReviewed = 0,
            DurationSec = 0,
            NewLearned = 0
        };

        _context.StudySessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        await _queueService.InitializeDueQueueAsync(session.Id, queue, cancellationToken).ConfigureAwait(false);

        var seenKey = GetSeenTermsKey(session.Id);
        var seenTermCardsKey = GetSeenTermCardsKey(session.Id);
        await _redis.KeyExpireAsync(seenKey, StudyQueueConstants.SessionDataTtl).ConfigureAwait(false);
        await _redis.KeyExpireAsync(seenTermCardsKey, StudyQueueConstants.SessionDataTtl).ConfigureAwait(false);

        // Calculate queue stats
        var stats = await CalculateQueueStatsAsync(queue, userId, projectId, cancellationToken);

        _logger.LogInformation(
            "Study session {SessionId} started for user {UserId}, project {ProjectId}. Queue size: {QueueSize}",
            session.Id, userId, projectId, queue.Count);

        return new StudySessionDto
        {
            Id = session.Id,
            ProjectId = session.ProjectId,
            Status = session.Status,
            StartTime = session.StartTime,
            CardsReviewed = session.CardsReviewed,
            QueueStats = stats
        };
    }

    /// <summary>
    /// Получает следующую карточку из очереди сессии (SR-LRN-02)
    /// </summary>
    public Task<CardStudyDto?> GetNextCardAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        GetNextCardCoreAsync(sessionId, userId, cancellationToken, learningDeferAttempts: 0);

    private async Task<CardStudyDto?> GetNextCardCoreAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken,
        int learningDeferAttempts)
    {
        // Verify session exists and belongs to user
        var studySession = await _context.StudySessions
            .Include(s => s.Project)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        if (studySession == null)
        {
            throw new KeyNotFoundException($"Session {sessionId} not found or access denied");
        }

        if (studySession.Status != "ACTIVE")
        {
            throw new InvalidOperationException($"Session {sessionId} is not active");
        }

        var cardId = await _queueService.PopDueCardIdAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (!cardId.HasValue)
        {
            var rebuilt = await RebuildSessionQueueAsync(studySession, cancellationToken).ConfigureAwait(false);
            if (rebuilt is { Count: > 0 })
            {
                await _queueService.InitializeDueQueueAsync(sessionId, rebuilt.ToList(), cancellationToken).ConfigureAwait(false);
                cardId = await _queueService.PopDueCardIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
            }
        }

        if (!cardId.HasValue)
        {
            var learnAheadCardId = await _queueService
                .FindLearnAheadCardIdAsync(studySession, cancellationToken)
                .ConfigureAwait(false);
            if (learnAheadCardId.HasValue)
            {
                _logger.LogInformation(
                    "Learn ahead: showing card {CardId} early (no other cards, due within {Minutes} min)",
                    learnAheadCardId.Value,
                    StudyQueueConstants.LearnAheadLimitMinutes);
                return await PresentStudyCardAsync(
                    studySession,
                    userId,
                    learnAheadCardId.Value,
                    cancellationToken,
                    learningDeferAttempts,
                    allowNotYetDueLearning: true).ConfigureAwait(false);
            }

            return null;
        }

        return await PresentStudyCardAsync(
            studySession,
            userId,
            cardId.Value,
            cancellationToken,
            learningDeferAttempts,
            allowNotYetDueLearning: false).ConfigureAwait(false);
    }

    private async Task<CardStudyDto?> PresentStudyCardAsync(
        StudySession studySession,
        Guid userId,
        Guid cardId,
        CancellationToken cancellationToken,
        int learningDeferAttempts,
        bool allowNotYetDueLearning)
    {
        // Load card
        var card = await _cardService.GetCardByIdAsync(cardId, userId, cancellationToken);
        if (card == null)
        {
            return await GetNextCardCoreAsync(studySession.Id, userId, cancellationToken, learningDeferAttempts);
        }

        // Load progress
        var cardProgress = await _context.UserCardProgresses
            .FirstOrDefaultAsync(
                p => p.CardId == cardId && p.UserId == userId && p.ProjectId == studySession.ProjectId,
                cancellationToken);

        // Anki: не показывать learning/relearning до Due, кроме learn-ahead window (и явного learn-ahead pop).
        if (!allowNotYetDueLearning
            && cardProgress != null
            && FsrsSettingsHelper.IsIntradayLearningState(cardProgress.State)
            && cardProgress.Due > DateTime.UtcNow)
        {
            if (learningDeferAttempts >= StudyQueueConstants.MaxLearningDeferAttempts)
            {
                var learnAheadCardId = await _queueService
                    .FindLearnAheadCardIdAsync(studySession, cancellationToken)
                    .ConfigureAwait(false);
                if (learnAheadCardId.HasValue)
                {
                    return await PresentStudyCardAsync(
                        studySession,
                        userId,
                        learnAheadCardId.Value,
                        cancellationToken,
                        learningDeferAttempts,
                        allowNotYetDueLearning: true).ConfigureAwait(false);
                }

                _logger.LogWarning(
                    "Defer limit reached for session {SessionId}; learning cards are scheduled in the future",
                    studySession.Id);
                return null;
            }

            await _queueService
                .ScheduleLearningAsync(studySession.Id, cardId, cardProgress.Due, cancellationToken)
                .ConfigureAwait(false);
            return await GetNextCardCoreAsync(studySession.Id, userId, cancellationToken, learningDeferAttempts + 1)
                .ConfigureAwait(false);
        }

        var seenKey = GetSeenTermsKey(studySession.Id);
        var seenTermCardsKey = GetSeenTermCardsKey(studySession.Id);

        // Sibling Burying (SR-LRN-04): bury sibling cards, but never bury the same
        // learning card when Anki-style learn-ahead shows it again in this session.
        // Prefer legacy lemma grouping when present; otherwise group by ProjectTermId (term-first cards).
        var siblingGroup = GetSiblingSessionRedisMember(card);
        if (siblingGroup != null)
        {
            var seenGroup = await IsSiblingGroupSeenAsync(studySession.Id, siblingGroup).ConfigureAwait(false);
            var seenCardValue = await GetSeenSiblingCardIdAsync(studySession.Id, siblingGroup).ConfigureAwait(false);
            var seenDifferentCard = seenCardValue.HasValue
                && Guid.TryParse(seenCardValue.ToString(), out var seenCardId)
                && seenCardId != cardId;

            if (seenGroup && seenDifferentCard)
            {
                if (cardProgress != null)
                {
                    cardProgress.Due = DateTime.UtcNow.AddDays(1);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation("Card {CardId} buried due to sibling group {SiblingGroup}", cardId, siblingGroup);
                return await GetNextCardCoreAsync(studySession.Id, userId, cancellationToken, learningDeferAttempts);
            }
        }

        // Mark sibling group as seen (Redis key name is historical: seen_lemmas).
        if (siblingGroup != null)
        {
            await _redis.SetAddAsync(seenKey, siblingGroup).ConfigureAwait(false);
            await _redis.HashSetAsync(seenTermCardsKey, siblingGroup, cardId.ToString()).ConfigureAwait(false);
            await _redis.KeyExpireAsync(seenKey, StudyQueueConstants.SessionDataTtl).ConfigureAwait(false);
            await _redis.KeyExpireAsync(seenTermCardsKey, StudyQueueConstants.SessionDataTtl).ConfigureAwait(false);
        }

        // Get SRS status
        var srsStatus = cardProgress != null
            ? MapStateToSrsStatus(cardProgress.State)
            : "NEW";

        // Calculate current interval
        int currentInterval = 0;
        if (cardProgress != null && cardProgress.Due > DateTime.UtcNow)
        {
            currentInterval = (int)(cardProgress.Due - DateTime.UtcNow).TotalDays;
        }

        // Calculate next intervals for display on buttons (Again, Hard, Good, Easy)
        var nextIntervals = await _fsrsPreviewService.GetButtonIntervalsAsync(
            cardProgress,
            studySession.Project?.FsrsSettings,
            cancellationToken).ConfigureAwait(false);

        // Count siblings (same lemma or same project term)
        var siblingsCount = await CountSiblingCardsAsync(card, cardId, cancellationToken);

        await _mediaService.FillCardMediaUrlsAsync(
            card.Note != null ? NoteFieldMapHelper.BuildCardMedia(card.Note.FieldValues) : null,
            cancellationToken).ConfigureAwait(false);

        // Next-card payload: canonical text + media come only from note.field_values.
        // target_index is computed from Expression + Word (not stored on Card). source_meta is derived from
        // SourceTitle + SourceUrl on the note.
        var fieldMap = card.Note?.FieldValues ?? new Dictionary<string, NoteFieldValue>();
        var expression = NoteFieldMapHelper.GetExpression(fieldMap);
        var surfaceWord = NoteFieldMapHelper.GetWord(fieldMap);
        TargetIndex targetIndex;
        try
        {
            targetIndex = NoteFieldMapHelper.CalculateTargetIndex(expression, surfaceWord);
        }
        catch (ArgumentException)
        {
            targetIndex = new TargetIndex { Start = 0, Len = 0 };
        }

        var title = NoteFieldMapHelper.GetString(fieldMap, SentenceMiningNoteType.SourceTitle);
        var url = NoteFieldMapHelper.GetString(fieldMap, SentenceMiningNoteType.SourceUrl);
        SourceMeta? sourceMeta = null;
        if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(url))
        {
            sourceMeta = new SourceMeta { Title = title ?? string.Empty, Url = url ?? string.Empty, Type = "web" };
        }

        return new CardStudyDto
        {
            Id = card.Id,
            Type = "SENTENCE_MINING",
            Content = new CardStudyContentDto
            {
                NoteId = card.Note?.Id ?? Guid.Empty,
                NoteTypeId = card.Note?.NoteTypeId ?? Guid.Empty,
                FieldValues = new Dictionary<string, NoteFieldValue>(fieldMap),
                ProjectTermId = card.Note?.ProjectTermId?.ToString("D"),
                TargetIndex = targetIndex
            },
            SourceMeta = sourceMeta,
            Media = NoteFieldMapHelper.BuildCardMedia(fieldMap),
            SrsState = new SrsStateDto
            {
                State = srsStatus,
                CurrentInterval = currentInterval,
                Step = cardProgress?.Step ?? 0,
                DueUtc = cardProgress?.Due,
            },
            NextIntervals = nextIntervals,
            SiblingsCount = siblingsCount
        };
    }

    /// <summary>
    /// Обрабатывает ответ пользователя и обновляет состояние карточки (SR-LRN-03)
    /// </summary>
    public async Task<ReviewResponseDto> SubmitReviewAsync(
        Guid sessionId,
        Guid userId,
        Guid cardId,
        int rating,
        int durationMs,
        string? userAnswer = null,
        CancellationToken cancellationToken = default)
    {
        // Verify session
        var studySession = await _context.StudySessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        if (studySession == null)
        {
            throw new KeyNotFoundException($"Session {sessionId} not found or access denied");
        }

        // Load card
        var card = await _cardService.GetCardByIdAsync(cardId, userId, cancellationToken);
        if (card == null)
        {
            throw new KeyNotFoundException($"Card {cardId} not found or access denied");
        }

        // Load or create progress
        var progress = await _context.UserCardProgresses
            .FirstOrDefaultAsync(
                p => p.CardId == cardId && p.UserId == userId && p.ProjectId == studySession.ProjectId,
                cancellationToken);

        bool isNewCard = progress == null;
        if (progress == null)
        {
            progress = new UserCardProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CardId = cardId,
                ProjectId = studySession.ProjectId,
                State = 0, // NEW
                Stability = 0,
                Difficulty = 0,
                Due = DateTime.UtcNow,
                ElapsedDays = 0,
                ScheduledDays = 0,
                Reps = 0,
                Lapses = 0,
                IsSuspended = false,
                LastReview = DateTime.UtcNow
            };
            _context.UserCardProgresses.Add(progress);
        }

        var snapshotBefore = StudyProgressUpdater.Capture(progress);

        // Load project FSRS settings
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == studySession.ProjectId, cancellationToken);

        if (project == null)
        {
            throw new KeyNotFoundException($"Project {studySession.ProjectId} not found");
        }

        // Calculate next state using FSRS (inclusive or built-in)
        var reviewAt = DateTime.UtcNow;
        var nextState = await _fsrsScheduler.GetNextStateAsync(
            progress,
            rating,
            reviewAt,
            durationMs,
            project.FsrsSettings,
            cancellationToken);

        StudyProgressUpdater.ApplyReview(progress, nextState, reviewAt, rating);

        if (rating == 1)
        {
            await _queueService.EnqueueDueFrontAsync(sessionId, cardId, cancellationToken).ConfigureAwait(false);
        }
        else if (FsrsSettingsHelper.IsIntradayLearningState(nextState.State))
        {
            var learnAheadCutoff = reviewAt.AddMinutes(StudyQueueConstants.LearnAheadLimitMinutes);
            if (nextState.Due <= learnAheadCutoff)
            {
                await _queueService
                    .ScheduleLearningAsync(sessionId, cardId, nextState.Due, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // Leech Detection (SR-LRN-05)
        bool isLeech = false;
        if (progress.Lapses >= 8)
        {
            progress.IsSuspended = true;
            isLeech = true;
            _logger.LogWarning("Card {CardId} marked as leech (lapses: {Lapses})", cardId, progress.Lapses);
        }

        // Create review log for undo
        var snapshotAfter = StudyProgressUpdater.Capture(progress);
        var reviewLog = new ReviewLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardId = cardId,
            SessionId = sessionId,
            Rating = (short)rating,
            StateBefore = snapshotBefore.State,
            StateAfter = snapshotAfter.State,
            StepBefore = snapshotBefore.Step,
            StepAfter = snapshotAfter.Step,
            RepsBefore = snapshotBefore.Reps,
            RepsAfter = snapshotAfter.Reps,
            LapsesBefore = snapshotBefore.Lapses,
            LapsesAfter = snapshotAfter.Lapses,
            ElapsedDaysBefore = snapshotBefore.ElapsedDays,
            ElapsedDaysAfter = snapshotAfter.ElapsedDays,
            ScheduledDaysBefore = snapshotBefore.ScheduledDays,
            ScheduledDaysAfter = snapshotAfter.ScheduledDays,
            LastReviewBefore = snapshotBefore.LastReview,
            LastReviewAfter = snapshotAfter.LastReview,
            DueBefore = snapshotBefore.Due,
            DueAfter = snapshotAfter.Due,
            StabilityBefore = snapshotBefore.Stability,
            StabilityAfter = snapshotAfter.Stability,
            DifficultyBefore = snapshotBefore.Difficulty,
            DifficultyAfter = snapshotAfter.Difficulty,
            ReviewDurationMs = durationMs,
            UserAnswer = null,
            AnswerValidationResult = null,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReviewLogs.Add(reviewLog);

        // Update session stats
        studySession.CardsReviewed += 1;
        if (isNewCard && nextState.State >= 1) // NEW -> LEARNING or higher
        {
            studySession.NewLearned += 1;
        }

        // Deep Skill Tracking + promote reader status when card graduates to Review
        if (card.ProjectTermId.HasValue)
        {
            var termId = card.ProjectTermId.Value;
            var termStatus = await _context.UserTermStatuses
                .FirstOrDefaultAsync(ts => ts.UserId == userId && ts.ProjectTermId == termId, cancellationToken);

            var nowUtc = DateTime.UtcNow;
            if (termStatus == null)
            {
                termStatus = new UserTermStatus
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProjectId = studySession.ProjectId,
                    ProjectTermId = termId,
                    Status = "NEW",
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc,
                };
                _context.UserTermStatuses.Add(termStatus);
            }

            var targetSkill = card.CardTemplate?.TargetSkill ?? "Reading";
            // Convert stability (days) to a 0-100 score. ~20 days = 100%.
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

            // MVP: Good/Easy + FSRS Review → KNOWN (white in reader). Never demote on Again.
            if (rating >= 3
                && nextState.State == 2
                && !string.Equals(termStatus.Status, "IGNORED", StringComparison.OrdinalIgnoreCase))
            {
                termStatus.Status = "KNOWN";
            }

            termStatus.UpdatedAt = nowUtc;
            termStatus.LastSeenAt = nowUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var intervalStr = StudyIntervalFormatter.FormatUntilDue(nextState.Due, DateTime.UtcNow);

        // Count buried siblings
        var buriedSiblings = 0;
        var reviewSiblingGroup = GetSiblingSessionRedisMember(card);
        if (reviewSiblingGroup != null)
        {
            var siblings = await ListSiblingCardIdsAsync(card, cardId, cancellationToken);

            if (await IsSiblingGroupSeenAsync(sessionId, reviewSiblingGroup).ConfigureAwait(false))
            {
                buriedSiblings = siblings.Count;
            }
        }

        return new ReviewResponseDto
        {
            CardId = cardId,
            NextReviewDate = nextState.Due,
            Interval = intervalStr,
            State = MapStateToSrsStatus(nextState.State),
            Stability = nextState.Stability,
            IsLeech = isLeech,
            BuriedSiblingsCount = buriedSiblings,
            AnswerValidation = null
        };
    }

    /// <summary>
    /// Отменяет последнее действие пользователя (SR-LRN-08)
    /// </summary>
    public async Task<UndoReviewDto> UndoReviewAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Verify session
        var studySession = await _context.StudySessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

        if (studySession == null)
        {
            throw new KeyNotFoundException($"Session {sessionId} not found or access denied");
        }

        // Find last review log
        var lastReview = await _context.ReviewLogs
            .Where(r => r.SessionId == sessionId && r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastReview == null)
        {
            throw new InvalidOperationException("No review to undo");
        }

        // Restore progress state
        var progress = await _context.UserCardProgresses
            .FirstOrDefaultAsync(
                p => p.CardId == lastReview.CardId
                    && p.UserId == userId
                    && p.ProjectId == studySession.ProjectId,
                cancellationToken);

        if (progress != null)
        {
            StudyProgressUpdater.Restore(progress, new StudyProgressSnapshot(
                lastReview.StateBefore,
                lastReview.StepBefore,
                lastReview.StabilityBefore,
                lastReview.DifficultyBefore,
                lastReview.DueBefore,
                lastReview.LastReviewBefore,
                lastReview.ElapsedDaysBefore,
                lastReview.ScheduledDaysBefore,
                lastReview.RepsBefore,
                lastReview.LapsesBefore));
        }

        await _queueService.RemoveFromQueuesAsync(sessionId, lastReview.CardId, cancellationToken).ConfigureAwait(false);
        if (progress != null && FsrsSettingsHelper.IsIntradayLearningState(progress.State) && progress.Due > DateTime.UtcNow)
        {
            await _queueService
                .ScheduleLearningAsync(sessionId, lastReview.CardId, progress.Due, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await _queueService.EnqueueDueFrontAsync(sessionId, lastReview.CardId, cancellationToken).ConfigureAwait(false);
        }

        // Update session stats
        studySession.CardsReviewed = Math.Max(0, studySession.CardsReviewed - 1);

        // Remove review log
        _context.ReviewLogs.Remove(lastReview);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Undo review for card {CardId} in session {SessionId}", lastReview.CardId, sessionId);

        return new UndoReviewDto
        {
            Success = true,
            RestoredCardId = lastReview.CardId,
            Message = "Previous review reverted. Card returned to queue."
        };
    }

    // ========== Private Helper Methods ==========

    /// <summary>
    /// Rebuilds a session queue from the database after service restart.
    /// Regenerates the full queue and removes cards already reviewed in this session.
    /// </summary>
    private async Task<Queue<Guid>?> RebuildSessionQueueAsync(
        StudySession session,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Rebuilding queue for session {SessionId} (lost after service restart)",
            session.Id);

        var deckIds = new List<Guid>();
        if (session.DeckId.HasValue)
        {
            deckIds = await CollectDeckIdsRecursiveAsync(session.DeckId.Value, session.UserId, cancellationToken);
        }
        else
        {
            deckIds = await _context.Decks
                .Where(d => d.ProjectId == session.ProjectId)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);
        }

        if (!deckIds.Any())
            return null;

        var userSettings = await _userSettingsService.GetUserSettingsAsync(session.UserId, cancellationToken);

        var fullQueue = await GenerateQueueAsync(
            session.UserId,
            session.ProjectId,
            deckIds,
            userSettings.DailyGoalNew,
            userSettings.DailyGoalReview,
            cancellationToken);

        _logger.LogInformation(
            "Rebuilt queue for session {SessionId}: {Remaining} remaining",
            session.Id, fullQueue.Count);

        return fullQueue.Count > 0 ? new Queue<Guid>(fullQueue) : null;
    }

    /// <summary>
    /// Рекурсивно собирает ID всех дочерних колод
    /// </summary>
    private async Task<List<Guid>> CollectDeckIdsRecursiveAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = new List<Guid> { deckId };

        var childDecks = await _context.Decks
            .Where(d => d.ParentDeckId == deckId)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        foreach (var childId in childDecks)
        {
            var grandchildren = await CollectDeckIdsRecursiveAsync(childId, userId, cancellationToken);
            result.AddRange(grandchildren);
        }

        return result;
    }

    /// <summary>
    /// Генерирует очередь карточек с приоритетами (SR-LRN-01)
    /// </summary>
    private async Task<List<Guid>> GenerateQueueAsync(
        Guid userId,
        Guid projectId,
        List<Guid> deckIds,
        int dailyGoalNew,
        int dailyGoalReview,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var queue = new List<Guid>();

        // 1. Lapses (highest priority) - cards that were forgotten
        var lapses = await _context.UserCardProgresses
            .Where(p => p.UserId == userId
                && p.ProjectId == projectId
                && deckIds.Contains(p.Card.DeckId)
                && p.State == 0 // NEW (relearning)
                && p.Due <= now
                && p.Lapses > 0
                && !p.IsSuspended)
            .Select(p => p.CardId)
            .ToListAsync(cancellationToken);

        queue.AddRange(lapses);

        // 2. Learning / Relearning — только уже due (как Anki). Learn-ahead для будущих due — когда очередь пуста.
        var learning = await _context.UserCardProgresses
            .Where(p => p.UserId == userId
                && p.ProjectId == projectId
                && deckIds.Contains(p.Card.DeckId)
                && (p.State == 1 || p.State == 3) // LEARNING or RELEARNING
                && p.Due <= now
                && !p.IsSuspended)
            .OrderBy(p => p.Due)
            .Select(p => p.CardId)
            .ToListAsync(cancellationToken);

        queue.AddRange(learning);

        // 3. Reviews - cards due for review
        var reviews = await _context.UserCardProgresses
            .Where(p => p.UserId == userId
                && p.ProjectId == projectId
                && deckIds.Contains(p.Card.DeckId)
                && p.State == 2 // REVIEW
                && p.Due <= now
                && !p.IsSuspended)
            .OrderBy(p => p.Due) // Most overdue first
            .Take(dailyGoalReview)
            .Select(p => p.CardId)
            .ToListAsync(cancellationToken);

        queue.AddRange(reviews);

        // 4. New cards - respect daily limit
        var lapsesSet = lapses.ToHashSet();

        // Cards with progress but never reviewed (e.g. suspend/unsuspend, sync) — must match DeckService NEW count.
        var unreviewedWithProgress = await _context.UserCardProgresses
            .Where(p => p.UserId == userId
                && p.ProjectId == projectId
                && deckIds.Contains(p.Card.DeckId)
                && p.State == 0
                && p.Reps == 0
                && p.Lapses == 0
                && !p.IsSuspended
                && !lapsesSet.Contains(p.CardId))
            .Select(p => p.CardId)
            .ToListAsync(cancellationToken);

        var existingProgressCardIds = await _context.UserCardProgresses
            .Where(p => p.UserId == userId && deckIds.Contains(p.Card.DeckId))
            .Select(p => p.CardId)
            .ToListAsync(cancellationToken);

        var existingProgressSet = existingProgressCardIds.ToHashSet();

        var newCardsWithoutProgress = await _context.Cards
            .Include(c => c.Deck)
            .Where(c => deckIds.Contains(c.DeckId)
                && !existingProgressSet.Contains(c.Id)
                && (c.CreatorId == userId || c.Deck.OwnerId == userId || c.Deck.IsPublic))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var newCards = unreviewedWithProgress
            .Concat(newCardsWithoutProgress)
            .Distinct()
            .Take(dailyGoalNew)
            .ToList();

        queue.AddRange(newCards);

        // Learn-ahead: cards due within the next window (not yet Due <= now) still belong in today's session.
        var learnAhead = await CollectLearnAheadCardIdsAsync(
            userId,
            projectId,
            deckIds,
            cancellationToken).ConfigureAwait(false);
        var queued = queue.ToHashSet();
        foreach (var cardId in learnAhead)
        {
            if (queued.Add(cardId))
                queue.Add(cardId);
        }

        // Shuffle while keeping lapses first
        var lapsesList = queue.Take(lapses.Count).ToList();
        var rest = queue.Skip(lapses.Count).ToList();
        
        // Shuffle the rest
        var random = new Random();
        rest = rest.OrderBy(x => random.Next()).ToList();

        return lapsesList.Concat(rest).ToList();
    }

    private async Task<List<Guid>> CollectLearnAheadCardIdsAsync(
        Guid userId,
        Guid projectId,
        List<Guid> deckIds,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(StudyQueueConstants.LearnAheadLimitMinutes);

        return await _context.UserCardProgresses
            .Where(p => p.UserId == userId
                && p.ProjectId == projectId
                && deckIds.Contains(p.Card.DeckId)
                && (p.State == 1 || p.State == 3)
                && p.Due > now
                && p.Due <= cutoff
                && !p.IsSuspended)
            .OrderBy(p => p.Due)
            .Select(p => p.CardId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Рассчитывает статистику очереди
    /// </summary>
    private async Task<QueueStatsDto> CalculateQueueStatsAsync(
        List<Guid> cardIds,
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!cardIds.Any())
        {
            return new QueueStatsDto();
        }

        var progresses = await _context.UserCardProgresses
            .Where(p => p.UserId == userId && p.ProjectId == projectId && cardIds.Contains(p.CardId))
            .ToListAsync(cancellationToken);

        var progressByCardId = progresses.ToDictionary(p => p.CardId);
        var newCount = cardIds.Count(id =>
        {
            progressByCardId.TryGetValue(id, out var progress);
            return progress == null || (progress.State == 0 && progress.Reps == 0 && progress.Lapses == 0);
        });

        var reviewCount = progresses.Count(p => p.State == 2); // REVIEW
        var learningCount = progresses.Count(p =>
            p.State == 1 || p.State == 3 || (p.State == 0 && p.Lapses > 0)); // LEARNING, RELEARNING, or lapse relearning

        return new QueueStatsDto
        {
            New = newCount,
            Review = reviewCount,
            Learning = learningCount
        };
    }

    /// <summary>
    /// Маппит состояние (short) в строковый статус SRS
    /// </summary>
    private string MapStateToSrsStatus(short state)
    {
        // Соответствие py-fsrs State: 1 Learning, 2 Review, 3 Relearning («mature» в Anki — не отдельный card state).
        return state switch
        {
            0 => "NEW",
            1 => "LEARNING",
            2 => "REVIEW",
            3 => "RELEARNING",
            _ => "NEW"
        };
    }

    private async Task<bool> IsSiblingGroupSeenAsync(Guid sessionId, string siblingGroup)
    {
        var seenKey = GetSeenTermsKey(sessionId);
        var legacyKey = GetLegacySeenLemmasKey(sessionId);
        return await _redis.SetContainsAsync(seenKey, siblingGroup).ConfigureAwait(false)
            || await _redis.SetContainsAsync(legacyKey, siblingGroup).ConfigureAwait(false);
    }

    private async Task<RedisValue> GetSeenSiblingCardIdAsync(Guid sessionId, string siblingGroup)
    {
        var seenTermCardsKey = GetSeenTermCardsKey(sessionId);
        var value = await _redis.HashGetAsync(seenTermCardsKey, siblingGroup).ConfigureAwait(false);
        if (value.HasValue)
            return value;

        return await _redis.HashGetAsync(GetLegacySeenLemmaCardsKey(sessionId), siblingGroup).ConfigureAwait(false);
    }

    private async Task EnsureStudySessionStatusColumnAsync(CancellationToken cancellationToken)
    {
        // Add the missing status column only for legacy schemas.
        var providerName = _context.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE internal.study_sessions ADD COLUMN status TEXT NOT NULL DEFAULT 'ACTIVE';",
                    cancellationToken);
            }
            catch (Exception ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
            {
                // Column already added by another request.
            }
            return;
        }

        await _context.Database.ExecuteSqlRawAsync(
            "ALTER TABLE internal.study_sessions ADD COLUMN IF NOT EXISTS status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE';",
            cancellationToken);
    }

    private static bool IsMissingStudySessionStatusColumn(Exception ex)
    {
        Exception? current = ex;
        while (current != null)
        {
            var message = current.Message;
            var mentionsStatusColumn = message.Contains("s.status", StringComparison.OrdinalIgnoreCase)
                || message.Contains("study_sessions.status", StringComparison.OrdinalIgnoreCase)
                || (message.Contains("status", StringComparison.OrdinalIgnoreCase)
                    && message.Contains("study_sessions", StringComparison.OrdinalIgnoreCase));
            var missingColumnSignal = message.Contains("no such column", StringComparison.OrdinalIgnoreCase)
                || message.Contains("does not exist", StringComparison.OrdinalIgnoreCase);

            if (mentionsStatusColumn && missingColumnSignal)
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    /// <summary>
    /// Redis set/hash member for sibling burying. Prefixed so namespaces never collide.
    /// </summary>
    private static string? GetSiblingSessionRedisMember(Card card)
    {
        if (card.ProjectTermId.HasValue)
            return "T:" + card.ProjectTermId.Value.ToString("D");
        return null;
    }

    private async Task<int> CountSiblingCardsAsync(Card card, Guid cardId, CancellationToken cancellationToken)
    {
        if (card.ProjectTermId.HasValue)
            return await _context.Cards.CountAsync(c => c.ProjectTermId == card.ProjectTermId && c.Id != cardId, cancellationToken);
        return 0;
    }

    private async Task<List<Guid>> ListSiblingCardIdsAsync(Card card, Guid cardId, CancellationToken cancellationToken)
    {
        if (card.ProjectTermId.HasValue)
            return await _context.Cards
                .Where(c => c.ProjectTermId == card.ProjectTermId && c.Id != cardId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
        return [];
    }

}
