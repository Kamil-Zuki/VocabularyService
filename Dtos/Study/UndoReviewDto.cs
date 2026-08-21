namespace VocabularyService.Dtos.Study;

/// <summary>
/// DTO для отмены последнего действия
/// </summary>
public class UndoReviewDto
{
    public bool Success { get; set; }
    public Guid RestoredCardId { get; set; }
    public string Message { get; set; } = string.Empty;
}
