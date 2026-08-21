using Microsoft.EntityFrameworkCore;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Dtos.Analytics;
using VocabularyService.Helpers;

namespace VocabularyService.Services;

/// <summary>
/// Сервис для аналитики и статистики
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly VocabularyServiceContext _context;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly IUserSettingsService _userSettingsService;

    // CEFR пороги (можно вынести в конфигурацию)
    private static readonly Dictionary<string, (int Min, int Max, string Title)> CefrLevels = new()
    {
        { "A1", (0, 500, "Beginner") },
        { "A2", (500, 1200, "Elementary") },
        { "B1", (1200, 2500, "Intermediate") },
        { "B2", (2500, 5000, "Upper Intermediate") },
        { "C1", (5000, 10000, "Advanced") },
        { "C2", (10000, int.MaxValue, "Proficient") }
    };

    public AnalyticsService(
        VocabularyServiceContext context,
        ILogger<AnalyticsService> logger,
        IUserSettingsService userSettingsService)
    {
        _context = context;
        _logger = logger;
        _userSettingsService = userSettingsService;
    }

    /// <summary>
    /// Получает оценку словарного запаса пользователя для проекта (SR-ANL-01)
    /// </summary>
    public async Task<VocabularyStatsDto> GetVocabularyStatsAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        // Verify project exists and belongs to user
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, cancellationToken);

        if (project == null)
        {
            throw new KeyNotFoundException($"Project {projectId} not found or access denied");
        }

        // Vocabulary statistics count individual words only, not phrases/expressions.
        var wordTermIds = (await _context.ProjectTerms
            .AsNoTracking()
            .Where(pt => pt.ProjectId == projectId && pt.Type == "WORD")
            .Select(pt => pt.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        // Term-first vocabulary stats merged with FSRS card progress (SR-ANL-01)
        var statusRows = await _context.UserTermStatuses
            .AsNoTracking()
            .Where(uts => uts.UserId == userId && uts.ProjectId == projectId && wordTermIds.Contains(uts.ProjectTermId))
            .Select(uts => new { uts.ProjectTermId, uts.Status })
            .ToListAsync(cancellationToken);

        var cardLinks = await (
            from c in _context.Cards.AsNoTracking()
            join d in _context.Decks.AsNoTracking() on c.DeckId equals d.Id
            where d.ProjectId == projectId && c.CreatorId == userId && c.ProjectTermId != null && wordTermIds.Contains(c.ProjectTermId.Value)
            select new { TermId = c.ProjectTermId!.Value, c.Id })
            .ToListAsync(cancellationToken);

        var cardIds = cardLinks.Select(x => x.Id).Distinct().ToList();
        var progressRows = cardIds.Count == 0
            ? []
            : await _context.UserCardProgresses
                .AsNoTracking()
                .Where(p => p.UserId == userId && cardIds.Contains(p.CardId))
                .Select(p => new { p.CardId, p.State, p.ScheduledDays })
                .ToListAsync(cancellationToken);

        var progressByCard = progressRows.ToDictionary(p => p.CardId);
        var cardStateByTerm = new Dictionary<Guid, (bool HasMature, bool HasReviewing)>();

        foreach (var link in cardLinks)
        {
            if (!cardStateByTerm.TryGetValue(link.TermId, out var state))
                state = (false, false);

            if (progressByCard.TryGetValue(link.Id, out var progress) && IsMatureFsrsProgress(progress.State, progress.ScheduledDays))
                state.HasMature = true;
            else
                state.HasReviewing = true;

            cardStateByTerm[link.TermId] = state;
        }

        var statusByTerm = statusRows.ToDictionary(r => r.ProjectTermId, r => r.Status);
        var allTermIds = statusByTerm.Keys.Union(cardStateByTerm.Keys).ToHashSet();

        var knownCount = 0;
        var savedCount = 0;
        var reviewingCount = 0;
        var newCount = 0;

        foreach (var termId in allTermIds)
        {
            statusByTerm.TryGetValue(termId, out var status);
            cardStateByTerm.TryGetValue(termId, out var cardState);

            if (string.Equals(status, "IGNORED", StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.Equals(status, "KNOWN", StringComparison.OrdinalIgnoreCase) || cardState.HasMature)
            {
                knownCount++;
                continue;
            }

            if (cardState.HasReviewing)
            {
                reviewingCount++;
                continue;
            }

            if (status is "SAVED" or "LINGQ" or "LEARNING")
            {
                savedCount++;
                continue;
            }

            newCount++;
        }

        var learningCount = savedCount + reviewingCount;
        var totalTerms = knownCount + learningCount + newCount;

        // Calculate CEFR level from known vocabulary
        var cefrLevel = CalculateCefrLevel(knownCount);

        // Estimate fluency (rough estimate: known words / typical native vocabulary)
        var estimatedFluency = Math.Min(100, (int)((double)knownCount / 20000 * 100));

        return new VocabularyStatsDto
        {
            ProjectId = projectId,
            TotalLemmas = totalTerms,
            MatureCount = knownCount,
            SavedCount = savedCount,
            ReviewingCount = reviewingCount,
            LearningCount = learningCount,
            NewCount = newCount,
            CefrLevel = cefrLevel,
            EstimatedFluency = estimatedFluency
        };
    }

    private static bool IsMatureFsrsProgress(short state, int scheduledDays) =>
        state == 2 && scheduledDays >= 21;

    /// <summary>
    /// Получает данные для календаря активности (heatmap) (SR-ANL-02)
    /// </summary>
    public async Task<HeatmapDto> GetHeatmapAsync(
        Guid userId,
        Guid? projectId,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (year < 1 || year > 9999)
            year = DateTime.UtcNow.Year;

        // Get user settings for rollover hour
        var userSettings = await _userSettingsService.GetUserSettingsAsync(userId, cancellationToken);

        // Calculate year boundaries
        var yearStart = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Query study sessions for the year. Project only columns that exist in DB per Docs/Entities.md
        // (study_sessions has no status column), so EF does not generate SELECT s.status.
        var query = _context.StudySessions
            .Where(s => s.UserId == userId
                && s.EndTime >= yearStart
                && s.EndTime < yearEnd);

        if (projectId.HasValue)
        {
            query = query.Where(s => s.ProjectId == projectId.Value);
        }

        var sessions = await query
            .OrderBy(s => s.EndTime)
            .Select(s => new { s.EndTime, s.CardsReviewed })
            .ToListAsync(cancellationToken);

        // Group by date (with rollover hour)
        var activityByDate = new Dictionary<DateOnly, int>();

        foreach (var session in sessions)
        {
            var date = TimezoneHelper.GetDateFromDateTime(session.EndTime, userSettings.RolloverHour);

            if (!activityByDate.ContainsKey(date))
            {
                activityByDate[date] = 0;
            }

            activityByDate[date] += session.CardsReviewed;
        }

        // Calculate total reviews
        var totalReviews = activityByDate.Values.Sum();

        // Calculate longest streak
        var longestStreak = CalculateLongestStreak(activityByDate.Keys.OrderBy(d => d).ToList());

        // Сумма времени изучения за год: ReviewLogs по user/year (и project при указании)
        long totalDurationMs;
        if (projectId.HasValue)
        {
            totalDurationMs = await (from r in _context.ReviewLogs
                    join s in _context.StudySessions on r.SessionId equals s.Id
                    where r.UserId == userId && s.ProjectId == projectId.Value
                        && r.CreatedAt >= yearStart && r.CreatedAt < yearEnd
                    select (long)r.ReviewDurationMs)
                .SumAsync(cancellationToken);
        }
        else
        {
            totalDurationMs = await _context.ReviewLogs
                .Where(r => r.UserId == userId && r.CreatedAt >= yearStart && r.CreatedAt < yearEnd)
                .SumAsync(r => (long)r.ReviewDurationMs, cancellationToken);
        }

        var totalTimeSpentSeconds = (int)(totalDurationMs / 1000);

        // Convert to ActivityDayDto with levels
        var activity = new Dictionary<DateOnly, ActivityDayDto>();
        foreach (var kvp in activityByDate)
        {
            activity[kvp.Key] = new ActivityDayDto
            {
                Count = kvp.Value,
                Level = CalculateActivityLevel(kvp.Value)
            };
        }

        return new HeatmapDto
        {
            ProjectId = projectId,
            Year = year,
            TotalReviews = totalReviews,
            LongestStreak = longestStreak,
            TotalTimeSpentSeconds = totalTimeSpentSeconds,
            Activity = activity
        };
    }

    /// <summary>
    /// Получает дневную сводку и информацию о серии (SR-ANL-03)
    /// </summary>
    public async Task<DailySummaryDto> GetDailySummaryAsync(
        Guid userId,
        int? timezoneOffset,
        CancellationToken cancellationToken = default)
    {
        // Get user settings
        var userSettings = await _userSettingsService.GetUserSettingsAsync(userId, cancellationToken);

        var now = DateTime.UtcNow;
        var dayStart = TimezoneHelper.GetDayStart(now, userSettings.RolloverHour, timezoneOffset);
        var dayEnd = TimezoneHelper.GetDayEnd(now, userSettings.RolloverHour, timezoneOffset);
        var today = TimezoneHelper.GetDateForUser(now, userSettings.RolloverHour, timezoneOffset);

        // Aggregate review logs for today (project only needed columns to avoid missing-column errors)
        var logData = await _context.ReviewLogs
            .Where(r => r.UserId == userId
                && r.CreatedAt >= dayStart
                && r.CreatedAt < dayEnd)
            .Select(r => new { r.StateBefore, r.StateAfter, r.ReviewDurationMs })
            .ToListAsync(cancellationToken);

        // Count new cards (StateBefore = 0, StateAfter >= 1)
        var newCardsCount = logData.Count(r => r.StateBefore == 0 && r.StateAfter >= 1);

        // Count reviews (all others)
        var reviewsCount = logData.Count(r => !(r.StateBefore == 0 && r.StateAfter >= 1));

        // Sum time spent
        var timeSpentSeconds = (int)(logData.Sum(r => r.ReviewDurationMs) / 1000.0);

        // Check goals
        var newCardsGoal = new GoalProgressDto
        {
            Current = newCardsCount,
            Target = userSettings.DailyGoalNew,
            IsCompleted = newCardsCount >= userSettings.DailyGoalNew
        };

        var reviewsGoal = new GoalProgressDto
        {
            Current = reviewsCount,
            Target = userSettings.DailyGoalReview,
            IsCompleted = reviewsCount >= userSettings.DailyGoalReview
        };

        // Check and update streak
        bool isStreakExtendedToday = false;
        var currentStreak = userSettings.CurrentStreak;

        // Check if goals are met
        bool goalsMet = newCardsGoal.IsCompleted || reviewsGoal.IsCompleted;

        if (goalsMet)
        {
            // Check if streak was already extended today
            var lastStudyDate = userSettings.LastStudyDate;

            if (lastStudyDate == null || lastStudyDate.Value < today)
            {
                // Extend streak
                currentStreak += 1;
                isStreakExtendedToday = true;

                // Update user settings (non-fatal: return summary even if persist fails)
                try
                {
                    userSettings.CurrentStreak = currentStreak;
                    userSettings.LastStudyDate = today;
                    if (currentStreak > userSettings.MaxStreak)
                        userSettings.MaxStreak = currentStreak;
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation(
                        "Streak extended for user {UserId}: {Streak} days",
                        userId, currentStreak);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Could not persist streak update for user {UserId}; returning summary with streak {Streak}",
                        userId, currentStreak);
                }
            }
            else if (lastStudyDate.Value == today)
            {
                // Streak already extended today
                isStreakExtendedToday = false;
            }
        }

        return new DailySummaryDto
        {
            Date = today,
            CurrentStreak = currentStreak,
            IsStreakExtendedToday = isStreakExtendedToday,
            TimeSpentSeconds = timeSpentSeconds,
            NewCards = newCardsGoal,
            Reviews = reviewsGoal
        };
    }

    /// <summary>
    /// Рассчитывает CEFR уровень на основе количества Mature лемм
    /// </summary>
    private CefrLevelDto CalculateCefrLevel(int matureCount)
    {
        string code = "A1";
        string title = "Beginner";
        int progressPercent = 0;
        int wordsToNextLevel = 0;

        foreach (var level in CefrLevels)
        {
            if (matureCount >= level.Value.Min && matureCount < level.Value.Max)
            {
                code = level.Key;
                title = level.Value.Title;
                
                var levelMin = level.Value.Min;
                var levelMax = level.Value.Max;
                var range = levelMax - levelMin;
                
                if (range > 0)
                {
                    progressPercent = (int)((double)(matureCount - levelMin) / range * 100);
                }
                else
                {
                    progressPercent = 100;
                }

                // Words to next level
                if (level.Key != "C2")
                {
                    wordsToNextLevel = levelMax - matureCount;
                }
                else
                {
                    wordsToNextLevel = 0; // C2 is the highest level
                }

                break;
            }
        }

        return new CefrLevelDto
        {
            Code = code,
            Title = title,
            ProgressPercent = progressPercent,
            WordsToNextLevel = wordsToNextLevel
        };
    }

    /// <summary>
    /// Рассчитывает уровень активности на основе количества повторений
    /// </summary>
    private int CalculateActivityLevel(int count)
    {
        if (count == 0) return 0;
        if (count <= 25) return 1;
        if (count <= 50) return 2;
        if (count <= 100) return 3;
        return 4;
    }

    /// <summary>
    /// Рассчитывает самую длинную серию дней с активностью
    /// </summary>
    private int CalculateLongestStreak(List<DateOnly> dates)
    {
        if (dates.Count == 0) return 0;

        int longestStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < dates.Count; i++)
        {
            var daysDiff = dates[i].DayNumber - dates[i - 1].DayNumber;
            
            if (daysDiff == 1)
            {
                currentStreak++;
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
            }
        }

        return longestStreak;
    }
}
