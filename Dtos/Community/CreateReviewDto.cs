namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для создания отзыва
/// </summary>
public class CreateReviewDto
{
    public Guid ProductId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
}
