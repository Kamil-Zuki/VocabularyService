namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для опубликованной колоды
/// </summary>
public class PublishedDeckDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public AuthorInfoDto Author { get; set; } = null!;
    public int CardCount { get; set; }
    public string LicenseType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
