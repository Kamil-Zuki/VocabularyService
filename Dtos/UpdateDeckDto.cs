namespace VocabularyService.Dtos;

/// <summary>
/// DTO для обновления колоды
/// </summary>
public class UpdateDeckDto
{
    /// <summary>
    /// Название колоды (опционально)
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Описание колоды (опционально)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL обложки колоды (опционально)
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Идентификатор родительской колоды (опционально)
    /// </summary>
    public Guid? ParentDeckId { get; set; }

    /// <summary>
    /// Флаг публичности колоды (опционально)
    /// </summary>
    public bool? IsPublic { get; set; }

    /// <summary>
    /// Политика вкладов (опционально)
    /// </summary>
    public string? ContributionPolicy { get; set; }

    /// <summary>
    /// Тип лицензии (опционально)
    /// </summary>
    public string? LicenseType { get; set; }
}

