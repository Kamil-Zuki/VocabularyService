namespace VocabularyService.Dtos.AI;

public class ExplainGrammarRequestDto
{
    public string Sentence { get; set; } = null!;
    public string TargetWord { get; set; } = null!;
    public string UserNativeLanguage { get; set; } = null!; // ISO 639-1
    public string? ContextPrompt { get; set; } // Опциональный дополнительный контекст
}
