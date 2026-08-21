namespace VocabularyService.Data.Entities;

public class SkillAssessmentLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid ProjectId { get; set; }

    /// <summary>
    /// reading, listening, writing, speaking
    /// </summary>
    public string Skill { get; set; } = null!;

    public int Score { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
