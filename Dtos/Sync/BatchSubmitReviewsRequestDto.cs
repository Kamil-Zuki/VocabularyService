namespace VocabularyService.Dtos.Sync;

/// <summary>
/// DTO для запроса пакетной отправки ответов (SR-SNC-03)
/// </summary>
public class BatchSubmitReviewsRequestDto
{
    public List<BatchReviewItemDto> Reviews { get; set; } = new();
}
