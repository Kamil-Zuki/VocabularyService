namespace VocabularyService.Data.Entities;

public class UserCefrProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>CEFR level code: A1 / A2 / B1 / B2 / C1 / C2</summary>
    public string CefrLevel { get; set; } = "A1";

    public int CompletedLessons { get; set; } = 0;

    public int TotalLessons { get; set; } = 0;

    public bool IsLevelCompleted { get; set; } = false;

    public DateTime? LevelCompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
