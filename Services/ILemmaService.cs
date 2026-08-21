using VocabularyService.Data.Entities;

namespace VocabularyService.Services;

public interface ILemmaService
{
    string Normalize(string word);

    Task<ProjectLemma?> ResolveForCardAsync(
        Guid projectId,
        string targetWord,
        Guid? mainCardId = null,
        CancellationToken cancellationToken = default);
}
