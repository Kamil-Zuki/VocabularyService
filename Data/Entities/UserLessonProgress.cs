namespace VocabularyService.Data.Entities;

public enum LessonStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}

public class UserLessonProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    public Guid LessonId { get; set; }
    public virtual Lesson Lesson { get; set; } = null!;

    public LessonStatus Status { get; set; } = LessonStatus.NotStarted;

    public Guid? AgentThreadId { get; set; }

    public int ScorePercent { get; set; } = 0;      // 0–100, set on completion
    public int TimeSpentSeconds { get; set; } = 0;  // Total time spent in lesson session

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
