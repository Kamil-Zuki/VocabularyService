namespace VocabularyService.Dtos.AI;

public class GenerateContextResponseDto
{
    public List<ContextSuggestionDto> Suggestions { get; set; } = new();
}
