namespace VocabularyService.Dtos.Sync;

/// <summary>
/// DTO для изменений в синхронизации
/// </summary>
public class SyncChangesDto
{
    public List<SyncDeckDto> Decks { get; set; } = new();
    public List<SyncCardDto> Cards { get; set; } = new();
    public List<SyncProgressDto> Progress { get; set; } = new();
}
