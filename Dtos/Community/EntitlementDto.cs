namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для права доступа (Entitlement)
/// </summary>
public class EntitlementDto
{
    public bool HasAccess { get; set; }
    public string Source { get; set; } = null!; // PURCHASE, CONTRIBUTION, FREE
    public DateTime? GrantedAt { get; set; }
}
