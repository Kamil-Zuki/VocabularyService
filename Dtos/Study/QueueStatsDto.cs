namespace VocabularyService.Dtos.Study;

/// <summary>
/// DTO для статистики очереди карточек
/// </summary>
public class QueueStatsDto
{
    public int New { get; set; }
    public int Review { get; set; }
    public int Learning { get; set; }
}
