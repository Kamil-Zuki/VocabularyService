namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для клонирования колоды
/// </summary>
public class ForkDeckDto
{
    public Guid DeckId { get; set; }
    public Guid TargetProjectId { get; set; }
    public string? NewTitle { get; set; }
}
