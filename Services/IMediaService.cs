using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Services;

public interface IMediaService
{
    Task<Guid> UploadImageAsync(Stream data, string contentType, CancellationToken cancellationToken = default);
    Task<Guid> UploadAudioAsync(Stream data, string contentType, CancellationToken cancellationToken = default);
    Task<string> GetDocumentUrlAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task FillCardMediaUrlsAsync(CardMedia? media, CancellationToken cancellationToken = default);
}
