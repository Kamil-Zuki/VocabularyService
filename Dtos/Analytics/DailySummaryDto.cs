namespace VocabularyService.Dtos.Analytics;

/// <summary>
/// DTO для дневной сводки
/// </summary>
public class DailySummaryDto
{
    public DateOnly Date { get; set; }
    public int CurrentStreak { get; set; }
    public bool IsStreakExtendedToday { get; set; }
    public int TimeSpentSeconds { get; set; }
    public GoalProgressDto NewCards { get; set; } = null!;
    public GoalProgressDto Reviews { get; set; } = null!;
}
