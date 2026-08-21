namespace VocabularyService.Dtos.Analytics;

/// <summary>
/// DTO для прогресса по цели
/// </summary>
public class GoalProgressDto
{
    public int Current { get; set; }
    public int Target { get; set; }
    public bool IsCompleted { get; set; }
}
