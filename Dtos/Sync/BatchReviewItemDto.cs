namespace VocabularyService.Dtos.Sync;

/// <summary>
/// DTO для одного элемента пакетной отправки ответов
/// </summary>
public class BatchReviewItemDto
{
    public Guid CardId { get; set; }
    public int Rating { get; set; } // 1-4 (Again, Hard, Good, Easy)
    public DateTime ReviewedAt { get; set; } // Время ответа (для офлайн-режима)
    public int DurationMs { get; set; } // Длительность ответа в миллисекундах
    public Guid? SessionId { get; set; } // Optional
    public string? UserAnswer { get; set; } // Optional (для валидации)
}
