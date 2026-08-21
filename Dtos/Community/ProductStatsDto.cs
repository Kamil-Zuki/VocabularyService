namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для статистики товара
/// </summary>
public class ProductStatsDto
{
    public Guid ProductId { get; set; }
    public int SalesCount { get; set; }
    public int ReviewsCount { get; set; }
    public double AverageRating { get; set; }
    public double? RetentionRate { get; set; } // Optional
}
