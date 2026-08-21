using VocabularyService.Dtos.Study;

namespace VocabularyService.Services;

/// <summary>
/// Интерфейс сервиса для работы с обучением
/// </summary>
public interface IStudyService
{
    /// <summary>
    /// Запускает новую сессию обучения и генерирует очередь карточек (SR-LRN-01)
    /// </summary>
    Task<StudySessionDto> StartStudySessionAsync(
        Guid userId,
        Guid projectId,
        Guid? deckId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает следующую карточку из очереди сессии (SR-LRN-02)
    /// </summary>
    Task<CardStudyDto?> GetNextCardAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обрабатывает ответ пользователя и обновляет состояние карточки (SR-LRN-03)
    /// </summary>
    Task<ReviewResponseDto> SubmitReviewAsync(
        Guid sessionId,
        Guid userId,
        Guid cardId,
        int rating,
        int durationMs,
        string? userAnswer = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отменяет последнее действие пользователя (SR-LRN-08)
    /// </summary>
    Task<UndoReviewDto> UndoReviewAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
