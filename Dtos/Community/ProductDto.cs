namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для товара (Product)
/// </summary>
public class ProductDto
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public AuthorInfoDto Author { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? DescriptionHtml { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!; // DRAFT, PUBLISHED, ARCHIVED
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int SalesCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
