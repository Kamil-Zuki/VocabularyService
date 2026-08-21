namespace VocabularyService.Dtos.Sync;

/// <summary>
/// DTO для запроса синхронизации данных (SR-SNC-01)
/// </summary>
public class SyncDataRequestDto
{
    public DateTime? LastSyncToken { get; set; } // Timestamp последней синхронизации
    public Guid? ProjectId { get; set; } // Optional, null = all projects
}
