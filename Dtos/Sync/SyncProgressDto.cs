namespace VocabularyService.Dtos.Sync;

/// <summary>
/// DTO для синхронизации прогресса пользователя по карточке
/// </summary>
public class SyncProgressDto
{
    public Guid CardId { get; set; }
    public Guid ProjectId { get; set; }
    public int State { get; set; } // 0=NEW, 1=LEARNING, 2=REVIEW, 3=RELEARNING (py-fsrs)
    public float Stability { get; set; }
    public float Difficulty { get; set; }
    public DateTime Due { get; set; }
    public int ElapsedDays { get; set; }
    public int ScheduledDays { get; set; }
    public int Reps { get; set; }
    public int Lapses { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime LastReview { get; set; }
}
