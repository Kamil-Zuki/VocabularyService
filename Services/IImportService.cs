namespace VocabularyService.Services;

public interface IImportService
{
    Task<Guid> CreateJobAsync(Guid userId, Guid deckId, Guid projectId, CancellationToken cancellationToken = default);
    Task<VocabularyService.Data.Entities.ImportJob?> GetJobAsync(Guid jobId, Guid userId, CancellationToken cancellationToken = default);
    Task ProcessImportJobAsync(Guid jobId, string documentId, string fileName, string configJson, CancellationToken cancellationToken = default);
}
