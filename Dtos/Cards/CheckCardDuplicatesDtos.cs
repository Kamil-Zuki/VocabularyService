using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos.Cards;

public class CheckCardDuplicatesRequestDto
{
    public Guid ProjectId { get; set; }
    /// <summary>Surface form of the term to check (word or phrase text).</summary>
    public string TermText { get; set; } = string.Empty;
}

public class CheckCardDuplicatesResponseDto
{
    public bool IsDuplicate { get; set; }
    public string? NormalizedSurface { get; set; }
    public List<CardDuplicatePreviewDto> ExistingCards { get; set; } = [];
}

public class CardDuplicatePreviewDto
{
    public string Id { get; set; } = string.Empty;
    public Guid NoteId { get; set; }
    public Guid NoteTypeId { get; set; }
    public Dictionary<string, NoteFieldValue> FieldValues { get; set; } = new();
    public string? ProjectTermId { get; set; }
    public string SrsStatus { get; set; } = "NEW";
    public bool HasAudio { get; set; }
    public string DeckTitle { get; set; } = string.Empty;
}
