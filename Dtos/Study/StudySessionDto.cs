namespace VocabularyService.Dtos.Study;

/// <summary>
/// DTO для представления сессии обучения
/// </summary>
public class StudySessionDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, COMPLETED
    public DateTime StartTime { get; set; }
    public int CardsReviewed { get; set; }
    public QueueStatsDto QueueStats { get; set; } = new();
}
