namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для создания товара
/// </summary>
public class CreateProductDto
{
    public Guid DeckId { get; set; }
    public string Title { get; set; } = null!;
    public string? DescriptionHtml { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
}
