namespace VocabularyService.Data.Entities;

/// <summary>
/// Справочник типов скиллов. Содержит пороги выполнения миссий.
/// Seed-данные: reading, listening, writing, speaking.
/// </summary>
public class SkillType
{
    public int Id { get; set; }

    /// <summary>Уникальный код скилла: "reading", "listening", "writing", "speaking"</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Отображаемое имя для UI</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Единица измерения значения: "minutes" | "exercises"</summary>
    public string Unit { get; set; } = "minutes";

    /// <summary>Порог Value, при котором миссия считается выполненной</summary>
    public int CompletionThreshold { get; set; }

    public virtual ICollection<UserSkillActivity> UserSkillActivities { get; set; } = [];
}
