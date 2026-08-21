namespace VocabularyService.Dtos.AI;

public class ExplainGrammarResponseDto
{
    public string Explanation { get; set; } = null!;
    public string? RelatedTopic { get; set; } // Связанная тема (например, "Passé Composé")
}
