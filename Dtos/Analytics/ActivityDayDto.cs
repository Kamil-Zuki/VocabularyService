namespace VocabularyService.Dtos.Analytics;

/// <summary>
/// DTO для дня активности в heatmap
/// </summary>
public class ActivityDayDto
{
    public int Count { get; set; } // Number of reviews
    public int Level { get; set; } // Intensity level 1-4
}
