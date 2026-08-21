using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos.Cards;

/// <summary>Creates a card from Sentence Mining (or compatible) field values on the note.</summary>
public class CreateCardDto
{
    public Guid UserId { get; set; }
    public Guid DeckId { get; set; }
    public Dictionary<string, NoteFieldValue> FieldValues { get; set; } = new();
}
