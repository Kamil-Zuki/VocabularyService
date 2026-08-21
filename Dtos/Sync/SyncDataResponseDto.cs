namespace VocabularyService.Dtos.Sync;

/// <summary>
/// DTO для ответа синхронизации данных (SR-SNC-01)
/// </summary>
public class SyncDataResponseDto
{
    public DateTime SyncToken { get; set; } // Новый токен синхронизации
    public bool RequiresFullSync { get; set; } // Флаг принудительной полной перезагрузки
    public SyncChangesDto Changes { get; set; } = new();
    public List<DeletedObjectInfoDto> DeletedObjects { get; set; } = new(); // Tombstones
    public bool HasMore { get; set; } // Есть ли еще данные для синхронизации
}
