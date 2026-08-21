namespace VocabularyService.Dtos;

/// <summary>
/// DTO для обновления настроек пользователя
/// </summary>
public class UpdateUserSettingsDto
{
    /// <summary>
    /// Час смены суток (0-23)
    /// </summary>
    public int? RolloverHour { get; set; }

    /// <summary>
    /// Дневная цель: новые карточки
    /// </summary>
    public int? DailyGoalNew { get; set; }

    /// <summary>
    /// Дневная цель: повторения
    /// </summary>
    public int? DailyGoalReview { get; set; }

    /// <summary>
    /// Язык интерфейса
    /// </summary>
    public string? InterfaceLanguage { get; set; }
}
