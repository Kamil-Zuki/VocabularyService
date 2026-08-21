namespace VocabularyService.Dtos;

/// <summary>
/// DTO для создания колоды
/// </summary>
public class CreateDeckDto
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Идентификатор проекта
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Идентификатор родительской колоды (опционально)
    /// </summary>
    public Guid? ParentDeckId { get; set; }

    /// <summary>
    /// Название колоды
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание колоды (опционально)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Флаг публичности колоды
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// URL обложки колоды (опционально)
    /// </summary>
    public string? CoverImageUrl { get; set; }
}

