using Microsoft.EntityFrameworkCore;
using Pvs.Content.Grpc;
using VocabularyService.Data;
using VocabularyService.Data.Entities;

namespace VocabularyService.Services;

public interface IAutopilotService
{
    Task<GetDailyAutopilotPlanResponse> GetDailyPlanAsync(Guid userId, Guid projectId, CancellationToken ct = default);
}

public class AutopilotService : IAutopilotService
{
    private readonly VocabularyServiceContext _dbContext;

    public AutopilotService(VocabularyServiceContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetDailyAutopilotPlanResponse> GetDailyPlanAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        var response = new GetDailyAutopilotPlanResponse();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // ── 0. Load today's skill activity (one query for all skills) ──────────
        // Key: SkillType.Code ("reading", "listening", "writing", "speaking")
        // Value: (Value accumulated today, CompletionThreshold from DB)
        var todayActivities = await _dbContext.UserSkillActivities
            .AsNoTracking()
            .Include(a => a.SkillType)
            .Where(a => a.UserId == userId && a.ProjectId == projectId && a.Date == today)
            .ToDictionaryAsync(a => a.SkillType.Code, a => new { a.Value, a.SkillType.CompletionThreshold }, ct);

        // ── 1. Next lesson in curriculum queue (highest priority) ─────────────
        var completedLessonIds = (await _dbContext.UserLessonProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.Status == LessonStatus.Completed)
            .Select(p => p.LessonId)
            .ToListAsync(ct))
            .ToHashSet();

        // Check if any lesson was completed TODAY (for knowledge_check completion)
        var completedLessonToday = await _dbContext.UserLessonProgresses
            .AsNoTracking()
            .AnyAsync(p => p.UserId == userId
                        && p.Status == LessonStatus.Completed
                        && p.CompletedAt.HasValue
                        && p.CompletedAt.Value.Date == DateTime.UtcNow.Date, ct);

        var nextLesson = await _dbContext.Lessons
            .AsNoTracking()
            .Where(l => !completedLessonIds.Contains(l.Id))
            .OrderBy(l => l.CefrLevel)
            .ThenBy(l => l.OrderIndex)
            .FirstOrDefaultAsync(ct);

        if (nextLesson != null)
        {
            response.Tasks.Add(new DailyAutopilotTask
            {
                TaskType = "lesson",
                Title = $"Next Lesson: {nextLesson.Title}",
                Description = $"Continue your {nextLesson.CefrLevel} curriculum. {nextLesson.Description}".Trim(),
                IsCompleted = false, // lesson IsCompleted = false until user finishes it during this session
                ActionUrl = $"/lessons/{nextLesson.Id}",
                DurationMinutes = nextLesson.EstimatedMinutes > 0 ? nextLesson.EstimatedMinutes : 20
            });
        }

        // ── 2. Get skill balance to decide weakest skill ──────────────────────
        var stats = await _dbContext.UserTermStatuses
            .Where(ts => ts.UserId == userId && ts.ProjectId == projectId && ts.Status == "SAVED")
            .GroupBy(ts => 1)
            .Select(g => new
            {
                AvgR = g.Average(ts => (double)ts.ReadingLevel),
                AvgL = g.Average(ts => (double)ts.ListeningLevel),
                AvgW = g.Average(ts => (double)ts.WritingLevel),
                AvgS = g.Average(ts => (double)ts.SpeakingLevel)
            })
            .FirstOrDefaultAsync(ct);

        // ── 3. Reading mission ────────────────────────────────────────────────
        var readingActivity = todayActivities.GetValueOrDefault("reading");
        var readingCompleted = readingActivity != null
            && readingActivity.Value >= readingActivity.CompletionThreshold;

        response.Tasks.Add(new DailyAutopilotTask
        {
            TaskType = "reading",
            Title = "Read for 15 minutes",
            Description = "Read any material in your library to passively absorb new words.",
            IsCompleted = readingCompleted,
            ActionUrl = "/library",
            DurationMinutes = 15
        });

        // ── 4. FSRS Daily Reviews ─────────────────────────────────────────────
        var dueCardsCount = await _dbContext.UserCardProgresses
            .CountAsync(p => p.UserId == userId && p.State != 0 && p.Due <= DateTime.UtcNow, ct);

        if (dueCardsCount > 0)
        {
            response.Tasks.Add(new DailyAutopilotTask
            {
                TaskType = "fsrs",
                Title = $"Review {dueCardsCount} flashcards",
                Description = "Keep your memory fresh by completing your daily FSRS queue.",
                IsCompleted = false, // if dueCardsCount > 0, it's not done; when done it won't appear
                ActionUrl = "/study",
                DurationMinutes = Math.Max(1, dueCardsCount / 10)
            });
        }

        // ── 5. Knowledge check based on weakest skill ─────────────────────────
        if (stats != null)
        {
            var skills = new Dictionary<string, double>
            {
                { "reading",   stats.AvgR },
                { "listening", stats.AvgL },
                { "writing",   stats.AvgW },
                { "speaking",  stats.AvgS }
            };

            var weakestSkill = skills.OrderBy(kv => kv.Value).First().Key;

            if (weakestSkill == "listening" || weakestSkill == "speaking")
            {
                var speakingActivity = todayActivities.GetValueOrDefault("speaking");
                var listeningActivity = todayActivities.GetValueOrDefault("listening");
                var challengeCompleted = completedLessonToday
                    || (speakingActivity != null && speakingActivity.Value >= speakingActivity.CompletionThreshold)
                    || (listeningActivity != null && listeningActivity.Value >= listeningActivity.CompletionThreshold);

                response.Tasks.Add(new DailyAutopilotTask
                {
                    TaskType = "knowledge_check",
                    Title = "Listening & Speaking Challenge",
                    Description = "Your listening/speaking skills need some work. Take an AI conversation exam.",
                    IsCompleted = challengeCompleted,
                    ActionUrl = "/lessons",
                    DurationMinutes = 10
                });
            }
            else
            {
                var writingActivity = todayActivities.GetValueOrDefault("writing");
                var writingCompleted = completedLessonToday
                    || (writingActivity != null && writingActivity.Value >= writingActivity.CompletionThreshold);

                response.Tasks.Add(new DailyAutopilotTask
                {
                    TaskType = "knowledge_check",
                    Title = "Writing Challenge",
                    Description = "Let's practice writing. The AI will ask you to translate and construct sentences.",
                    IsCompleted = writingCompleted,
                    ActionUrl = "/lessons",
                    DurationMinutes = 10
                });
            }
        }
        else
        {
            // Fallback: no skill stats yet — prompt initial check
            response.Tasks.Add(new DailyAutopilotTask
            {
                TaskType = "knowledge_check",
                Title = "Initial Knowledge Check",
                Description = "Take an AI test to establish your baseline skills.",
                IsCompleted = completedLessonToday,
                ActionUrl = "/lessons",
                DurationMinutes = 10
            });
        }

        return response;
    }
}
