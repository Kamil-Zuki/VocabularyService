namespace VocabularyService.Services.Study;

/// <summary>Snapshot of progress fields for undo.</summary>
public sealed record StudyProgressSnapshot(
    short State,
    int Step,
    float Stability,
    float Difficulty,
    DateTime Due,
    DateTime LastReview,
    int ElapsedDays,
    int ScheduledDays,
    int Reps,
    int Lapses);
