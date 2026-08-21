namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для публикации колоды
/// </summary>
public class PublishDeckDto
{
    public Guid DeckId { get; set; }
    public string LicenseType { get; set; } = null!; // FREE or COMMERCIAL
}
