namespace VocabularyService.Dtos.Analytics;

/// <summary>
/// DTO для оценки словарного запаса пользователя
/// </summary>
public class VocabularyStatsDto
{
    public Guid ProjectId { get; set; }
    public int TotalLemmas { get; set; }
    public int MatureCount { get; set; }
    /// <summary>Saved terms without linked FSRS cards.</summary>
    public int SavedCount { get; set; }
    /// <summary>Terms with non-mature linked FSRS cards.</summary>
    public int ReviewingCount { get; set; }
    /// <summary>Active learning total (Saved + In Review) for backward compatibility.</summary>
    public int LearningCount { get; set; }
    public int NewCount { get; set; }
    public CefrLevelDto CefrLevel { get; set; } = null!;
    public int EstimatedFluency { get; set; } // 0-100
}
