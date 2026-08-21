namespace VocabularyService.Dtos.Analytics;

/// <summary>
/// DTO для календаря активности (heatmap)
/// </summary>
public class HeatmapDto
{
    public Guid? ProjectId { get; set; } // null if all projects
    public int Year { get; set; }
    public int TotalReviews { get; set; }
    public int LongestStreak { get; set; }
    /// <summary>Сумма времени изучения за год (секунды), из ReviewLogs.ReviewDurationMs</summary>
    public int TotalTimeSpentSeconds { get; set; }
    public Dictionary<DateOnly, ActivityDayDto> Activity { get; set; } = new();
}
