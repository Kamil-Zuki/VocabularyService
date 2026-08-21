using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Pvs.Content.Grpc;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using Google.Protobuf.WellKnownTypes;

namespace VocabularyService.Services;

public class LessonGrpcService : LessonService.LessonServiceBase
{
    private readonly VocabularyServiceContext _context;

    public LessonGrpcService(VocabularyServiceContext context)
    {
        _context = context;
    }

    public override async Task<GetLessonsResponse> GetLessons(GetLessonsRequest request, ServerCallContext context)
    {
        var lessons = await _context.Lessons
            .AsNoTracking()
            .OrderBy(l => l.CefrLevel)
            .ThenBy(l => l.OrderIndex)
            .ToListAsync(context.CancellationToken);

        var progressQuery = _context.UserLessonProgresses
            .AsNoTracking()
            .Where(p => p.UserId == Guid.Parse(request.UserId));
            
        var progresses = await progressQuery.ToDictionaryAsync(p => p.LessonId, context.CancellationToken);

        var response = new GetLessonsResponse();
        foreach (var lesson in lessons)
        {
            var dto = new LessonWithProgressDto
            {
                Lesson = MapToDto(lesson)
            };

            if (progresses.TryGetValue(lesson.Id, out var progress))
            {
                dto.Progress = MapToDto(progress);
            }

            response.Lessons.Add(dto);
        }

        return response;
    }

    public override async Task<GetLessonResponse> GetLesson(GetLessonRequest request, ServerCallContext context)
    {
        var lessonId = Guid.Parse(request.LessonId);
        var lesson = await _context.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lessonId, context.CancellationToken);

        if (lesson == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Lesson not found"));

        var progress = await _context.UserLessonProgresses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.LessonId == lessonId && p.UserId == Guid.Parse(request.UserId), context.CancellationToken);

        var response = new GetLessonResponse
        {
            LessonWithProgress = new LessonWithProgressDto
            {
                Lesson = MapToDto(lesson),
                Progress = progress != null ? MapToDto(progress) : null
            }
        };

        return response;
    }

    public override async Task<StartLessonResponse> StartLesson(StartLessonRequest request, ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var lessonId = Guid.Parse(request.LessonId);

        // Load lesson to check prerequisites
        var lesson = await _context.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lessonId, context.CancellationToken);

        if (lesson == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Lesson not found"));

        // Unlock check: if UnlocksAfterLessonId is set, verify the prerequisite is completed
        if (lesson.UnlocksAfterLessonId.HasValue)
        {
            var prerequisiteCompleted = await _context.UserLessonProgresses
                .AsNoTracking()
                .AnyAsync(
                    p => p.UserId == userId
                      && p.LessonId == lesson.UnlocksAfterLessonId.Value
                      && p.Status == LessonStatus.Completed,
                    context.CancellationToken);

            if (!prerequisiteCompleted)
                throw new RpcException(new Status(StatusCode.PermissionDenied,
                    "You must complete the previous lesson before starting this one."));
        }

        var progress = await _context.UserLessonProgresses
            .FirstOrDefaultAsync(p => p.LessonId == lessonId && p.UserId == userId, context.CancellationToken);

        if (progress == null)
        {
            progress = new UserLessonProgress
            {
                UserId = userId,
                LessonId = lessonId,
                Status = LessonStatus.InProgress,
                AgentThreadId = Guid.Parse(request.AgentThreadId),
                StartedAt = DateTime.UtcNow
            };
            _context.UserLessonProgresses.Add(progress);
        }
        else
        {
            if (progress.Status == LessonStatus.Completed)
            {
                // Reset to allow replay
                progress.Status = LessonStatus.InProgress;
                progress.StartedAt = DateTime.UtcNow;
                progress.CompletedAt = null;
                progress.ScorePercent = 0;
                progress.TimeSpentSeconds = 0;
            }
            else if (progress.Status == LessonStatus.NotStarted)
            {
                progress.Status = LessonStatus.InProgress;
                progress.StartedAt = DateTime.UtcNow;
            }
            progress.AgentThreadId = Guid.Parse(request.AgentThreadId);
        }

        await _context.SaveChangesAsync(context.CancellationToken);

        return new StartLessonResponse
        {
            Progress = MapToDto(progress)
        };
    }

    public override async Task<Empty> CompleteLesson(CompleteLessonRequest request, ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var lessonId = Guid.Parse(request.LessonId);

        var progress = await _context.UserLessonProgresses
            .FirstOrDefaultAsync(p => p.LessonId == lessonId && p.UserId == userId);

        if (progress != null && progress.Status != LessonStatus.Completed)
        {
            progress.Status = LessonStatus.Completed;
            progress.CompletedAt = DateTime.UtcNow;
            progress.ScorePercent = Math.Clamp(request.ScorePercent, 0, 100);
            progress.TimeSpentSeconds = Math.Max(0, request.TimeSpentSeconds);
            await _context.SaveChangesAsync();

            // Upsert CEFR-level progress for this user
            var lesson = await _context.Lessons
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson != null)
                await UpsertCefrProgressAsync(userId, lesson.CefrLevel, context.CancellationToken);
        }

        return new Empty();
    }

    public override async Task<Empty> SetPlacementLevel(SetPlacementLevelRequest request, ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var targetLevel = request.CefrLevel.ToUpperInvariant();

        var cefrOrder = new List<string> { "A1", "A2", "B1", "B2", "C1", "C2" };
        var targetIndex = cefrOrder.IndexOf(targetLevel);
        
        if (targetIndex < 0) 
        {
            // If invalid, do nothing
            return new Empty();
        }

        var levelsToComplete = targetIndex > 0 ? cefrOrder.Take(targetIndex).ToList() : new List<string>();
        var levelsToReset = cefrOrder.Skip(targetIndex).ToList();

        // Get all lessons for those levels
        var lessonsToComplete = await _context.Lessons
            .AsNoTracking()
            .Where(l => levelsToComplete.Contains(l.CefrLevel))
            .ToListAsync(context.CancellationToken);

        var lessonIds = lessonsToComplete.Select(l => l.Id).ToList();

        // Get existing progress
        var existingProgresses = await _context.UserLessonProgresses
            .Where(p => p.UserId == userId && lessonIds.Contains(p.LessonId))
            .ToDictionaryAsync(p => p.LessonId, context.CancellationToken);

        foreach (var lesson in lessonsToComplete)
        {
            if (existingProgresses.TryGetValue(lesson.Id, out var progress))
            {
                if (progress.Status != LessonStatus.Completed)
                {
                    progress.Status = LessonStatus.Completed;
                    progress.CompletedAt = DateTime.UtcNow;
                    progress.ScorePercent = 100;
                }
            }
            else
            {
                _context.UserLessonProgresses.Add(new UserLessonProgress
                {
                    UserId = userId,
                    LessonId = lesson.Id,
                    Status = LessonStatus.Completed,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    ScorePercent = 100,
                    TimeSpentSeconds = 120 // Assumed skipped time
                });
            }
        }

        await _context.SaveChangesAsync(context.CancellationToken);

        // Reset progress for levels >= targetLevel
        if (levelsToReset.Any())
        {
            var lessonsToReset = await _context.Lessons
                .AsNoTracking()
                .Where(l => levelsToReset.Contains(l.CefrLevel))
                .Select(l => l.Id)
                .ToListAsync(context.CancellationToken);

            var progressesToReset = await _context.UserLessonProgresses
                .Where(p => p.UserId == userId && lessonsToReset.Contains(p.LessonId))
                .ToListAsync(context.CancellationToken);

            if (progressesToReset.Any())
            {
                _context.UserLessonProgresses.RemoveRange(progressesToReset);
                await _context.SaveChangesAsync(context.CancellationToken);
            }
        }

        // Recompute UserCefrProgress for ALL levels
        foreach (var level in cefrOrder)
        {
            await UpsertCefrProgressAsync(userId, level, context.CancellationToken);
        }

        return new Empty();
    }

    public override async Task<Empty> SubmitKnowledgeCheckResult(SubmitKnowledgeCheckResultRequest request, ServerCallContext context)
    {
        var termGuids = request.TermIds.Select(Guid.Parse).ToList();
        var statuses = await _context.UserTermStatuses
            .Where(ts => ts.UserId == Guid.Parse(request.UserId) && ts.ProjectId == Guid.Parse(request.ProjectId) && termGuids.Contains(ts.ProjectTermId))
            .ToListAsync(context.CancellationToken);

        foreach (var status in statuses)
        {
            if (request.ReadingScore > 0) status.ReadingLevel = Math.Max(status.ReadingLevel, request.ReadingScore);
            if (request.ListeningScore > 0) status.ListeningLevel = Math.Max(status.ListeningLevel, request.ListeningScore);
            if (request.WritingScore > 0) status.WritingLevel = Math.Max(status.WritingLevel, request.WritingScore);
            if (request.SpeakingScore > 0) status.SpeakingLevel = Math.Max(status.SpeakingLevel, request.SpeakingScore);
            
            status.UpdatedAt = DateTime.UtcNow;
        }

        var userIdGuid = Guid.Parse(request.UserId);
        var projectIdGuid = Guid.Parse(request.ProjectId);

        if (request.ReadingScore > 0)
        {
            _context.SkillAssessmentLogs.Add(new SkillAssessmentLog
            {
                UserId = userIdGuid,
                ProjectId = projectIdGuid,
                Skill = "reading",
                Score = request.ReadingScore
            });
        }
        if (request.ListeningScore > 0)
        {
            _context.SkillAssessmentLogs.Add(new SkillAssessmentLog
            {
                UserId = userIdGuid,
                ProjectId = projectIdGuid,
                Skill = "listening",
                Score = request.ListeningScore
            });
        }
        if (request.WritingScore > 0)
        {
            _context.SkillAssessmentLogs.Add(new SkillAssessmentLog
            {
                UserId = userIdGuid,
                ProjectId = projectIdGuid,
                Skill = "writing",
                Score = request.WritingScore
            });
        }
        if (request.SpeakingScore > 0)
        {
            _context.SkillAssessmentLogs.Add(new SkillAssessmentLog
            {
                UserId = userIdGuid,
                ProjectId = projectIdGuid,
                Skill = "speaking",
                Score = request.SpeakingScore
            });
        }

        await _context.SaveChangesAsync(context.CancellationToken);
        return new Empty();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Recalculates and upserts the UserCefrProgress row for a given user + CEFR level.
    /// Called after a lesson is marked Completed.
    /// </summary>
    private async Task UpsertCefrProgressAsync(Guid userId, string cefrLevel, CancellationToken ct)
    {
        var allLessonsAtLevel = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.CefrLevel == cefrLevel)
            .Select(l => l.Id)
            .ToListAsync(ct);

        var totalLessons = allLessonsAtLevel.Count;

        var completedLessons = await _context.UserLessonProgresses
            .AsNoTracking()
            .CountAsync(p => p.UserId == userId
                          && allLessonsAtLevel.Contains(p.LessonId)
                          && p.Status == LessonStatus.Completed, ct);

        var isLevelCompleted = totalLessons > 0 && completedLessons >= totalLessons;

        var row = await _context.UserCefrProgresses
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CefrLevel == cefrLevel, ct);

        if (row == null)
        {
            row = new UserCefrProgress
            {
                UserId = userId,
                CefrLevel = cefrLevel,
                CompletedLessons = completedLessons,
                TotalLessons = totalLessons,
                IsLevelCompleted = isLevelCompleted,
                LevelCompletedAt = isLevelCompleted ? DateTime.UtcNow : null,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserCefrProgresses.Add(row);
        }
        else
        {
            row.CompletedLessons = completedLessons;
            row.TotalLessons = totalLessons;
            row.UpdatedAt = DateTime.UtcNow;

            if (isLevelCompleted && !row.IsLevelCompleted)
            {
                row.IsLevelCompleted = true;
                row.LevelCompletedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    private static LessonDto MapToDto(Lesson lesson) => new LessonDto
    {
        Id = lesson.Id.ToString(),
        Title = lesson.Title,
        Description = lesson.Description ?? string.Empty,
        Category = lesson.Category ?? string.Empty,
        Difficulty = lesson.Difficulty ?? string.Empty,
        ContentMarkdown = lesson.ContentMarkdown ?? string.Empty,
        ColorCssClass = lesson.ColorCssClass,
        SystemPrompt = lesson.SystemPrompt ?? string.Empty,
        CefrLevel = lesson.CefrLevel ?? string.Empty,
        OrderIndex = lesson.OrderIndex,
        UnlocksAfterLessonId = lesson.UnlocksAfterLessonId?.ToString(),
        TargetSkills = lesson.TargetSkills ?? string.Empty,
        EstimatedMinutes = lesson.EstimatedMinutes
    };

    private static UserLessonProgressDto MapToDto(UserLessonProgress progress) => new UserLessonProgressDto
    {
        Id = progress.Id.ToString(),
        UserId = progress.UserId.ToString(),
        LessonId = progress.LessonId.ToString(),
        Status = (int)progress.Status,
        AgentThreadId = progress.AgentThreadId?.ToString(),
        StartedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(progress.StartedAt, DateTimeKind.Utc)),
        CompletedAt = progress.CompletedAt.HasValue
            ? Timestamp.FromDateTime(DateTime.SpecifyKind(progress.CompletedAt.Value, DateTimeKind.Utc))
            : null,
        ScorePercent = progress.ScorePercent,
        TimeSpentSeconds = progress.TimeSpentSeconds
    };
}
