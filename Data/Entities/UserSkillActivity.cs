namespace VocabularyService.Data.Entities;

/// <summary>
/// Накопленная активность пользователя по конкретному скиллу за день.
/// Уникально по (UserId, ProjectId, Date, SkillTypeId).
/// Value накапливается через upsert (ON CONFLICT DO UPDATE SET value = value + excluded.value).
/// </summary>
public class UserSkillActivity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ProjectId { get; set; }

    /// <summary>UTC-дата (без времени)</summary>
    public DateOnly Date { get; set; }

    /// <summary>FK → SkillTypes.Id</summary>
    public int SkillTypeId { get; set; }

    /// <summary>
    /// Накопленное значение за день.
    /// Для reading/listening — минуты. Для writing/speaking — кол-во выполненных упражнений.
    /// </summary>
    public int Value { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual SkillType SkillType { get; set; } = null!;
}
