namespace VocabularyService.Dtos.Study;

/// <summary>
/// DTO для отправки оценки карточки
/// </summary>
public class SubmitReviewDto
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public Guid CardId { get; set; }
    public int Rating { get; set; } // 1=Again, 2=Hard, 3=Good, 4=Easy
    public int DurationMs { get; set; }
    public string? UserAnswer { get; set; } // Опциональный текстовый ответ пользователя
}
