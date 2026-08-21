using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos.AI;

public class ContextSuggestionDto
{
    public string Sentence { get; set; } = null!;
    public string Translation { get; set; } = null!;
    public string TargetWord { get; set; } = null!;
    public TargetIndex TargetIndex { get; set; } = null!;
}
