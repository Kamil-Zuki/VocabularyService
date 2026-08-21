#nullable enable
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Services.Study;

public sealed class FsrsPreviewService : IFsrsPreviewService
{
    private readonly IFsrsScheduler _fsrsScheduler;

    public FsrsPreviewService(IFsrsScheduler fsrsScheduler)
    {
        _fsrsScheduler = fsrsScheduler;
    }

    public async Task<Dictionary<int, string>> GetButtonIntervalsAsync(
        UserCardProgress? progress,
        FsrsSettings? settings,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<int, string>();
        var now = DateTime.UtcNow;
        var previewSettings = FsrsSettingsHelper.CloneWithoutFuzz(settings);

        var effective = CloneProgressForPreview(progress, now);

        for (var rating = 1; rating <= 4; rating++)
        {
            var next = await _fsrsScheduler
                .GetNextStateAsync(effective, rating, now, 0, previewSettings, cancellationToken)
                .ConfigureAwait(false);
            result[rating] = StudyIntervalFormatter.FormatUntilDue(next.Due, now);
        }

        return result;
    }

    private static UserCardProgress CloneProgressForPreview(UserCardProgress? progress, DateTime now)
    {
        if (progress == null)
        {
            return new UserCardProgress
            {
                UserId = Guid.Empty,
                CardId = Guid.Empty,
                State = 0,
                Step = 0,
                Stability = 0,
                Difficulty = 0,
                Due = now,
                LastReview = now,
                Reps = 0,
                Lapses = 0,
            };
        }

        return new UserCardProgress
        {
            UserId = progress.UserId,
            CardId = progress.CardId,
            ProjectId = progress.ProjectId,
            State = progress.State,
            Step = progress.Step,
            Stability = progress.Stability,
            Difficulty = progress.Difficulty,
            Due = progress.Due,
            LastReview = progress.LastReview,
            Reps = progress.Reps,
            Lapses = progress.Lapses,
            ElapsedDays = progress.ElapsedDays,
            ScheduledDays = progress.ScheduledDays,
        };
    }
}
