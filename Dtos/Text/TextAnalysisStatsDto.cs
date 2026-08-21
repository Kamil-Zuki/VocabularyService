namespace VocabularyService.Dtos.Text;

public class TextAnalysisStatsDto
{
    public int UniqueWords { get; set; }
    public double KnownPercentage { get; set; } // 0.0 - 1.0
    public int NewWordsCount { get; set; }
    public int LearningWordsCount { get; set; }
    public int KnownWordsCount { get; set; }
}
