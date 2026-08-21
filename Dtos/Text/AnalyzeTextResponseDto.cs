namespace VocabularyService.Dtos.Text;

public class AnalyzeTextResponseDto
{
    public List<TextTokenDto> Tokens { get; set; } = new();
    public List<TextPhraseDto> Phrases { get; set; } = new();
    public TextAnalysisStatsDto Stats { get; set; } = new();
}
