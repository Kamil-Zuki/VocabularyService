#nullable enable
using VocabularyService.Data.Entities;
using VocabularyService.Services;

namespace VocabularyService.Services.Study;

internal static class StudyProgressUpdater
{
    public static StudyProgressSnapshot Capture(UserCardProgress progress) =>
        new(
            progress.State,
            progress.Step,
            progress.Stability,
            progress.Difficulty,
            progress.Due,
            progress.LastReview,
            progress.ElapsedDays,
            progress.ScheduledDays,
            progress.Reps,
            progress.Lapses);

    public static void Restore(UserCardProgress progress, StudyProgressSnapshot snapshot)
    {
        progress.State = snapshot.State;
        progress.Step = snapshot.Step;
        progress.Stability = snapshot.Stability;
        progress.Difficulty = snapshot.Difficulty;
        progress.Due = snapshot.Due;
        progress.LastReview = snapshot.LastReview;
        progress.ElapsedDays = snapshot.ElapsedDays;
        progress.ScheduledDays = snapshot.ScheduledDays;
        progress.Reps = snapshot.Reps;
        progress.Lapses = snapshot.Lapses;
    }

    public static void ApplyReview(
        UserCardProgress progress,
        FsrsNextState nextState,
        DateTime reviewAt,
        int rating)
    {
        var stateBefore = progress.State;
        var elapsedDays = Math.Max(0, (int)(reviewAt.Date - progress.LastReview.Date).TotalDays);
        var scheduledDays = FsrsSettingsHelper.IsIntradayLearningState(nextState.State)
            ? 0
            : Math.Max(0, (int)(nextState.Due.Date - reviewAt.Date).TotalDays);

        progress.State = nextState.State;
        progress.Step = nextState.Step;
        progress.Stability = nextState.Stability;
        progress.Difficulty = nextState.Difficulty;
        progress.Due = nextState.Due;
        progress.LastReview = reviewAt;
        progress.ElapsedDays = elapsedDays;
        progress.ScheduledDays = scheduledDays;
        progress.Reps += 1;

        // Anki: lapse count increases on Again for review/relearning cards.
        if (rating == 1 && stateBefore is 2 or 3)
            progress.Lapses += 1;
    }
}
