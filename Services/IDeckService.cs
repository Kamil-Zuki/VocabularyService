using VocabularyService.Data.Entities;
using VocabularyService.Dtos;

namespace VocabularyService.Services;

/// <summary>
/// Интерфейс сервиса для работы с колодами
/// </summary>
public interface IDeckService
{
    /// <summary>
    /// Фильтр библиотеки: Мои / Скачанные / Публичные (для Library UI).
    /// </summary>
    public enum LibraryFilterKind
    {
        Unspecified = 0,
        Mine = 1,
        Downloaded = 2,
        Public = 3
    }

    /// <summary>
    /// Получает дерево колод для проекта
    /// </summary>
    /// <param name="projectId">Идентификатор проекта</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="libraryFilter">Опциональный фильтр: Мои / Скачанные / Публичные</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список корневых узлов дерева колод</returns>
    Task<List<DeckTreeItem>> GetDeckTreeAsync(
        Guid projectId,
        Guid userId,
        LibraryFilterKind libraryFilter = LibraryFilterKind.Unspecified,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создает новую колоду
    /// </summary>
    /// <param name="dto">DTO с данными для создания колоды</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Созданная колода</returns>
    Task<Deck> CreateDeckAsync(
        CreateDeckDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет колоду
    /// </summary>
    /// <param name="deckId">Идентификатор колоды</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="dto">DTO с данными для обновления</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Обновленная колода</returns>
    Task<Deck> UpdateDeckAsync(
        Guid deckId,
        Guid userId,
        UpdateDeckDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет колоду
    /// </summary>
    /// <param name="deckId">Идентификатор колоды</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task DeleteDeckAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает колоду по идентификатору
    /// </summary>
    /// <param name="deckId">Идентификатор колоды</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Колода или null, если не найдена</returns>
    Task<Deck?> GetDeckByIdAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает детальную информацию о колоде со статистикой карточек для пользователя
    /// </summary>
    /// <param name="deckId">Идентификатор колоды</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Детали колоды или null, если не найдена/нет доступа</returns>
    Task<DeckDetailDto?> GetDeckDetailAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Вспомогательный класс для представления узла дерева колод
/// </summary>
public class DeckTreeItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CardCount { get; set; }
    public List<DeckTreeItem> Children { get; set; } = new();
    /// <summary>Владелец колоды (для фильтра «Мои»).</summary>
    public Guid OwnerId { get; set; }
    /// <summary>Публичная колода (для фильтра «Публичные»).</summary>
    public bool IsPublic { get; set; }
    /// <summary>Колода создана как копия/покупка (для фильтра «Скачанные» и бейджа Purchased).</summary>
    public Guid? ForkedFromId { get; set; }
    /// <summary>URL обложки колоды.</summary>
    public string? CoverImageUrl { get; set; }
    /// <summary>Статистика карточек для текущего пользователя.</summary>
    public DeckDetailStatsDto Stats { get; set; } = new();
}

