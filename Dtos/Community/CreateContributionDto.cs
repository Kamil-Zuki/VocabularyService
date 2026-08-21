using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для создания предложения
/// </summary>
public class CreateContributionDto
{
    public Guid DeckId { get; set; }
    public Guid? CardId { get; set; } // Required for EDIT/DELETE
    public string Type { get; set; } = null!; // EDIT, ADD, DELETE
    public ContributionPayload Content { get; set; } = null!; // Required for EDIT/ADD
    public string? Comment { get; set; }
}
