namespace VocabularyService.Dtos.Analytics;

/// <summary>
/// DTO для уровня CEFR
/// </summary>
public class CefrLevelDto
{
    public string Code { get; set; } = "A1"; // A1, A2, B1, B2, C1, C2
    public string Title { get; set; } = "Beginner";
    public int ProgressPercent { get; set; } // 0-100
    public int WordsToNextLevel { get; set; }
}
