namespace VocabularyService.Dtos.Sync;

/// <summary>
/// DTO для информации об удаленном объекте (Tombstone)
/// </summary>
public class DeletedObjectInfoDto
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = null!; // DECK, CARD, PROJECT
}
