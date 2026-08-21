using VocabularyService.Dtos.Sync;

namespace VocabularyService.Services;

/// <summary>
/// Интерфейс сервиса для синхронизации данных
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Получает дельту изменений для синхронизации (SR-SNC-01)
    /// </summary>
    Task<SyncDataResponseDto> SyncDataAsync(
        Guid userId,
        SyncDataRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обрабатывает пакетную отправку офлайн-ответов (SR-SNC-03)
    /// </summary>
    Task<BatchSubmitReviewsResponseDto> BatchSubmitReviewsAsync(
        Guid userId,
        BatchSubmitReviewsRequestDto request,
        CancellationToken cancellationToken = default);
}
