using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos;

/// <summary>
/// DTO для создания проекта
/// </summary>
public class CreateProjectDto
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Название проекта
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Код родного языка (ISO 639-1)
    /// </summary>
    public string SourceLang { get; set; } = string.Empty;

    /// <summary>
    /// Код изучаемого языка (ISO 639-1)
    /// </summary>
    public string TargetLang { get; set; } = string.Empty;

    /// <summary>
    /// Настройки FSRS (опционально)
    /// </summary>
    public FsrsSettings? FsrsSettings { get; set; }
}

