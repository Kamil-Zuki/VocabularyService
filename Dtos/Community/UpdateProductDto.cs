namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для обновления товара
/// </summary>
public class UpdateProductDto
{
    public string? Title { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; } // DRAFT, PUBLISHED, ARCHIVED
}
