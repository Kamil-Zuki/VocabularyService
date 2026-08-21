namespace VocabularyService.Dtos.Study;

/// <summary>
/// DTO для запроса старта сессии обучения
/// </summary>
public class StartStudySessionDto
{
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? DeckId { get; set; }
}
