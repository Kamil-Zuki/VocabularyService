using VocabularyService.Dtos.Analytics;

namespace VocabularyService.Services;

/// <summary>
/// Интерфейс сервиса для аналитики
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Получает оценку словарного запаса пользователя для проекта (SR-ANL-01)
    /// </summary>
    Task<VocabularyStatsDto> GetVocabularyStatsAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает данные для календаря активности (heatmap) (SR-ANL-02)
    /// </summary>
    Task<HeatmapDto> GetHeatmapAsync(
        Guid userId,
        Guid? projectId,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает дневную сводку и информацию о серии (SR-ANL-03)
    /// </summary>
    Task<DailySummaryDto> GetDailySummaryAsync(
        Guid userId,
        int? timezoneOffset,
        CancellationToken cancellationToken = default);
}
