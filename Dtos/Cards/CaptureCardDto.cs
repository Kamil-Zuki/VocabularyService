using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos.Cards;

public class CaptureCardDto
{
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public Dictionary<string, NoteFieldValue> FieldValues { get; set; } = new();
    public string? ScreenshotBase64 { get; set; }
    /// <summary>When null, capture targets the project &quot;Inbox&quot; deck.</summary>
    public Guid? DeckId { get; set; }
}
