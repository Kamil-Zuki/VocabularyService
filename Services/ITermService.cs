using VocabularyService.Data.Entities;
using VocabularyService.Dtos.Terms;

namespace VocabularyService.Services;

public interface ITermService
{
    Task<UserTermStatus> CreateOrUpdateAsync(
        Guid userId,
        Guid projectId,
        string surfaceText,
        string type,
        string? languageHint,
        string? statusHint,
        string? meaning,
        string? firstSentence,
        string? firstSourceTitle,
        string? firstSourceUrl,
        CancellationToken cancellationToken = default);

    Task<UserTermStatus> MarkKnownAsync(
        Guid userId,
        Guid projectId,
        string surfaceText,
        string type,
        string? languageHint,
        CancellationToken cancellationToken = default);

    Task<UserTermStatus> IgnoreAsync(
        Guid userId,
        Guid projectId,
        string surfaceText,
        string type,
        string? languageHint,
        CancellationToken cancellationToken = default);

    Task<int> BulkMarkKnownAsync(
        Guid userId,
        Guid projectId,
        IReadOnlyList<BulkMarkKnownItemDto> items,
        string? languageHint,
        CancellationToken cancellationToken = default);

    Task<(ProjectTerm Term, UserTermStatus? Status)> GetDetailsAsync(
        Guid userId,
        Guid projectId,
        string surfaceText,
        string type,
        CancellationToken cancellationToken = default);

    Task<(ProjectTerm? Term, List<Card> Cards)> SearchDuplicatesAsync(
        Guid userId,
        Guid projectId,
        string surfaceText,
        string type,
        CancellationToken cancellationToken = default);

    /// <summary>Список терминов проекта для пользователя (keyset cursor по UpdatedAt DESC, TermId ASC).</summary>
    Task<(IReadOnlyList<ProjectTermListRow> Items, int TotalCount)> ListProjectTermsAsync(
        Guid userId,
        Guid projectId,
        string? statusFilter,
        string? typeFilter,
        string? searchQuery,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Связать сохранённую карточку с термином и поднять статус LingQ.</summary>
    Task LinkCardWordTermAsync(Guid userId, Card card, CancellationToken cancellationToken);

    /// <summary>Удаляет демо-карточки импорта и связанные term/status rows.</summary>
    Task<PurgeDemoImportResult> PurgeDemoImportDataAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default);
}
