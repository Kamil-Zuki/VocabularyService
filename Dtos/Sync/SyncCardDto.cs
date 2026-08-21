using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos.Sync;

/// <summary>Card payload for offline sync (note is canonical).</summary>
public class SyncCardDto
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public Guid CreatorId { get; set; }
    public Guid NoteId { get; set; }
    public Dictionary<string, NoteFieldValue> FieldValues { get; set; } = new();
    public string SearchDocument { get; set; } = string.Empty;
    public Guid? ProjectTermId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
