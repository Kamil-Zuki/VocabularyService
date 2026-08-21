namespace VocabularyService.Dtos.Sync;

/// <summary>
/// DTO для ответа пакетной отправки ответов (SR-SNC-03)
/// </summary>
public class BatchSubmitReviewsResponseDto
{
    public int ProcessedCount { get; set; } // Количество успешно обработанных ревью
    public int FailedCount { get; set; } // Количество неудачных ревью
    public List<Guid> FailedCardIds { get; set; } = new(); // ID карточек, которые не удалось обработать
}
