using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos.Sync;

/// <summary>
/// DTO для синхронизации колоды
/// </summary>
public class SyncDeckDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentDeckId { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsPublic { get; set; }
    public string ContributionPolicy { get; set; } = null!; // OPEN, RESTRICTED, CLOSED
    public string LicenseType { get; set; } = null!;
    public Guid? ForkedFromId { get; set; }
    public int CardCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
