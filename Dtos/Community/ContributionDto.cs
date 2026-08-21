using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для предложения (Contribution)
/// </summary>
public class ContributionDto
{
    public Guid Id { get; set; }
    public Guid TargetDeckId { get; set; }
    public Guid? TargetCardId { get; set; }
    public AuthorInfoDto Author { get; set; } = null!;
    public string Type { get; set; } = null!; // EDIT, ADD, DELETE
    public string Status { get; set; } = null!; // PENDING, MERGED, REJECTED
    public ContributionPayload Content { get; set; } = null!;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Информация об авторе
/// </summary>
public class AuthorInfoDto
{
    public Guid UserId { get; set; }
    public string? DisplayName { get; set; }
}
