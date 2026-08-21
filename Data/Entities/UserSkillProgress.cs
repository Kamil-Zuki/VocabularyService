using System.Text.Json;
using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Data.Entities;

/// <summary>
/// Постоянный прогресс пользователя по конкретному языковому навыку в рамках проекта.
/// Хранит текущий уровень, суммарное накопленное значение и произвольные skill-специфичные данные (jsonb).
/// Пример Metadata для "reading": {"lastBookId": "...", "lastPage": 42}
/// </summary>
public class UserSkillProgress
{
    public Guid UserId { get; set; }

    public Guid ProjectId { get; set; }

    public int SkillTypeId { get; set; }

    /// <summary>Текущий уровень навыка (0–100). Рассчитывается на основе TotalValue и алгоритма прогресса.</summary>
    public int Level { get; set; }

    /// <summary>Суммарное накопленное значение за всё время (в единицах, указанных в SkillType.Unit).</summary>
    public int TotalValue { get; set; }

    /// <summary>Произвольные данные, специфичные для навыка. Для "reading": {"lastBookId", "lastPage"}.</summary>
    public Dictionary<string, JsonElement>? Metadata { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual SkillType SkillType { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;
}
