namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для принятия решения по предложению
/// </summary>
public class ResolveContributionDto
{
    public string Status { get; set; } = null!; // MERGED or REJECTED
    public string? ResolutionComment { get; set; }
}
