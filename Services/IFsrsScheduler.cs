using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Services;

/// <summary>
/// Результат расчёта следующего состояния карточки по FSRS.
/// </summary>
public record FsrsNextState(float Stability, float Difficulty, DateTime Due, short State, int Step);

/// <summary>
/// Абстракция планировщика FSRS: расчёт следующего состояния карточки по оценке пользователя.
/// </summary>
public interface IFsrsScheduler
{
    /// <summary>
    /// Вычисляет следующее состояние карточки после ревью.
    /// </summary>
    /// <param name="progress">Текущий прогресс карточки.</param>
    /// <param name="rating">Оценка 1–4 (Again, Hard, Good, Easy).</param>
    /// <param name="reviewAt">Момент ревью (UTC).</param>
    /// <param name="durationMs">Длительность ревью в миллисекундах.</param>
    /// <param name="settings">Настройки FSRS проекта (опционально; inclusive их не использует).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<FsrsNextState> GetNextStateAsync(
        UserCardProgress progress,
        int rating,
        DateTime reviewAt,
        int durationMs,
        FsrsSettings? settings,
        CancellationToken cancellationToken = default);
}
