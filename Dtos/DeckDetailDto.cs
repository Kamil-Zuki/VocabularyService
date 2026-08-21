namespace VocabularyService.Dtos;

/// <summary>
/// DTO детальной информации о колоде (для GET /api/decks/{id})
/// </summary>
public class DeckDetailDto
{
    /// <summary>
    /// Идентификатор колоды
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Название колоды
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание колоды
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Идентификатор родительской колоды (для хлебных крошек)
    /// </summary>
    public Guid? ParentDeckId { get; set; }

    /// <summary>
    /// Идентификатор проекта
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Идентификатор владельца колоды
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// URL обложки колоды
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Публичная колода
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Политика вкладов (OPEN, RESTRICTED, CLOSED)
    /// </summary>
    public string ContributionPolicy { get; set; } = string.Empty;

    /// <summary>
    /// Тип лицензии (PRIVATE, FREE_ATTRIBUTION, COMMERCIAL, COMMERCIAL_DERIVATIVE)
    /// </summary>
    public string LicenseType { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор колоды-источника, если скачано/куплено
    /// </summary>
    public Guid? ForkedFromId { get; set; }

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Количество карточек в колоде
    /// </summary>
    public int CardCount { get; set; }

    /// <summary>
    /// Статистика по карточкам колоды для текущего пользователя
    /// </summary>
    public DeckDetailStatsDto Stats { get; set; } = new();
}

/// <summary>
/// Статистика карточек в колоде: New / Learning / Due / Total
/// </summary>
public class DeckDetailStatsDto
{
    /// <summary>
    /// Количество новых карточек (Repetitions == 0)
    /// </summary>
    public int NewCardsCount { get; set; }

    /// <summary>
    /// Количество карточек в изучении (Interval &lt; 1 day)
    /// </summary>
    public int LearningCardsCount { get; set; }

    /// <summary>
    /// Количество карточек к повторению (State REVIEW, Due &lt;= UtcNow)
    /// </summary>
    public int DueCardsCount { get; set; }

    /// <summary>
    /// Карточки, которые сессия Study может показать сейчас (due + learn-ahead window)
    /// </summary>
    public int StudyableNowCount { get; set; }

    /// <summary>
    /// Общее количество карточек в колоде
    /// </summary>
    public int TotalCardsCount { get; set; }
}
