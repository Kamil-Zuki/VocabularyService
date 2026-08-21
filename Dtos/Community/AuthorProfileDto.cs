namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для профиля автора
/// </summary>
public class AuthorProfileDto
{
    public Guid AuthorId { get; set; }
    public string? DisplayName { get; set; }
    public int PublishedDecksCount { get; set; }
    public int TotalSales { get; set; }
    public double AverageRating { get; set; }
}
