using VocabularyService.Data.Entities;
using VocabularyService.Dtos.Cards;

namespace VocabularyService.Services;

/// <summary>
/// Интерфейс сервиса для работы с карточками
/// </summary>
public interface ICardService
{
    /// <summary>
    /// Создает новую карточку вручную
    /// </summary>
    Task<Card> CreateCardAsync(CreateCardDto dto, CancellationToken cancellationToken = default);

    /// <summary>Deck owner imports/creates a card authored by another user (e.g. contribution merge).</summary>
    Task<Card> CreateCardAsDeckOwnerAsync(Guid deckOwnerUserId, CreateCardDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет, есть ли в проекте карточки с той же леммой.
    /// </summary>
    Task<CheckCardDuplicatesResponseDto> CheckDuplicatesAsync(
        Guid userId,
        CheckCardDuplicatesRequestDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Захватывает карточку из внешнего источника (расширение)
    /// </summary>
    Task<Card> CaptureCardAsync(CaptureCardDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает карточку по ID с проверкой прав доступа
    /// </summary>
    Task<Card?> GetCardByIdAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет существующую карточку
    /// </summary>
    Task<Card> UpdateCardAsync(Guid cardId, Guid userId, UpdateCardDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет карточку
    /// </summary>
    Task DeleteCardAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Полнотекстовый поиск по карточкам пользователя
    /// </summary>
    Task<(List<Card> Items, int TotalCount)> SearchCardsAsync(
        Guid userId, 
        string query, 
        Guid? projectId, 
        Guid? deckId, 
        int pageNumber, 
        int pageSize, 
        List<string>? srsStatuses = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает список карточек в конкретной колоде
    /// </summary>
    Task<(List<Card> Items, int TotalCount)> GetCardsByDeckAsync(
        Guid userId, 
        Guid deckId, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Массовое создание карточек (Импорт)
    /// </summary>
    Task<List<Card>> BulkCreateCardsAsync(Guid userId, Guid deckId, List<CreateCardDto> dtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Приостанавливает обучение карточки
    /// </summary>
    Task SuspendCardAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возобновляет обучение карточки
    /// </summary>
    Task UnsuspendCardAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает облегченные превью карточек
    /// </summary>
    Task<List<Card>> GetCardPreviewsAsync(Guid userId, List<Guid> cardIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает статус SRS для карточки и пользователя
    /// </summary>
    Task<string> GetSrsStatusAsync(Guid cardId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Массово удаляет карточки пользователя
    /// </summary>
    Task<int> BulkDeleteCardsAsync(Guid userId, IReadOnlyList<Guid> cardIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Перемещает карточки в другую колоду
    /// </summary>
    Task<int> MoveCardsAsync(Guid userId, IReadOnlyList<Guid> cardIds, Guid deckId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сбрасывает прогресс SRS для карточек
    /// </summary>
    Task<int> ResetCardProgressAsync(Guid userId, IReadOnlyList<Guid> cardIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает карточки с высоким количеством ошибок (leeches)
    /// </summary>
    Task<(List<Card> Items, int TotalCount)> GetLeechCardsAsync(
        Guid userId, Guid projectId, int threshold, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает карточки без изображения или аудио
    /// </summary>
    Task<(List<Card> Items, int TotalCount)> GetCardsMissingMediaAsync(
        Guid userId, Guid projectId, string? mediaType, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
