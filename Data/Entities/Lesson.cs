namespace VocabularyService.Data.Entities;

public class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // e.g. "Grammar & Structure", "Real-Life Situations"
    public string Difficulty { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public string? ColorCssClass { get; set; }

    // Curriculum / CEFR fields
    public string CefrLevel { get; set; } = "B1";    // A1 / A2 / B1 / B2 / C1 / C2
    public int OrderIndex { get; set; } = 0;          // Sort order within the CEFR level (1-N)
    public Guid? UnlocksAfterLessonId { get; set; }   // Previous lesson that must be completed first (null = always unlocked)
    public string TargetSkills { get; set; } = "R,W"; // Comma-separated: R / L / W / S
    public int EstimatedMinutes { get; set; } = 20;   // Approximate lesson duration

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<UserLessonProgress> UserProgresses { get; set; } = new List<UserLessonProgress>();
}
