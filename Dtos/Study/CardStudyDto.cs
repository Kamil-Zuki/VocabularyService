using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos.Study;

/// <summary>
/// DTO для карточки в режиме обучения. Контент только из полей заметки; подсветка — производная.
/// </summary>
public class CardStudyDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "SENTENCE_MINING";
    public CardStudyContentDto Content { get; set; } = new();
    public SourceMeta? SourceMeta { get; set; }
    public CardMedia? Media { get; set; }
    public SrsStateDto SrsState { get; set; } = new();
    public Dictionary<int, string> NextIntervals { get; set; } = new();
    public int SiblingsCount { get; set; }
}

/// <summary>
/// Контент для study: полный payload заметки + вычисленный target index.
/// </summary>
public class CardStudyContentDto
{
    public Guid NoteId { get; set; }
    public Guid NoteTypeId { get; set; }
    public Dictionary<string, NoteFieldValue> FieldValues { get; set; } = new();
    public string? ProjectTermId { get; set; }
    public TargetIndex TargetIndex { get; set; } = new();
}
