using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos.Cards;

/// <summary>Partial update: merged into the note's field map by key.</summary>
public class UpdateCardDto
{
    public Dictionary<string, NoteFieldValue>? FieldValues { get; set; }
}
