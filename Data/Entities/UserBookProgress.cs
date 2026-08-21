using System;

namespace VocabularyService.Data.Entities;

/// <summary>
/// Прогресс пользователя при чтении книг/документов в ридере.
/// Заменяет хранение позиции в JSON Metadata.
/// </summary>
public class UserBookProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid ProjectId { get; set; }

    /// <summary>
    /// Внешний идентификатор книги (например, Id из MediaService или внешний URL/URN).
    /// </summary>
    public string BookId { get; set; } = string.Empty;

    /// <summary>
    /// Процент прочитанного (от 0 до 100).
    /// </summary>
    public float ProgressPercent { get; set; }

    /// <summary>
    /// Строковый локатор последней позиции (например, номер страницы для PDF или EPUB CFI).
    /// </summary>
    public string? LastPositionLocator { get; set; }

    /// <summary>
    /// Название последней прочитанной главы.
    /// </summary>
    public string? LastChapter { get; set; }

    public bool IsFinished { get; set; }

    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Project Project { get; set; } = null!;
}
