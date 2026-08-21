#nullable enable
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Services.Study;

public interface IFsrsPreviewService
{
    Task<Dictionary<int, string>> GetButtonIntervalsAsync(
        UserCardProgress? progress,
        FsrsSettings? settings,
        CancellationToken cancellationToken = default);
}
