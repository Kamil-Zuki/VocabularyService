namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для отзыва на товар
/// </summary>
public class ProductReviewDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public bool IsVerified { get; set; }
    public string? AuthorReply { get; set; }
    public DateTime CreatedAt { get; set; }
}
