using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Dtos.Community;

/// <summary>
/// DTO для отображения различий (Diff) между оригинальной и предложенной карточкой
/// </summary>
public class ContributionDiffDto
{
    public ContributionPayload? OriginalCard { get; set; } // Null for ADD type
    public ContributionPayload ProposedCard { get; set; } = null!;
    public List<string> ChangedFields { get; set; } = new();
    public bool IsConflict { get; set; } // True if card was modified after contribution creation
}
