using VocabularyService.Data.Entities;
using VocabularyService.Dtos;
using JsonTypes = VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Services;

/// <summary>
/// Сервис для работы с проектами
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Создает новый проект и системную колоду "Inbox"
    /// </summary>
    /// <param name="dto">DTO с данными для создания проекта</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Созданный проект</returns>
    Task<Project> CreateProjectAsync(
        CreateProjectDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет, не превышен ли лимит проектов для пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>True, если лимит не превышен</returns>
    Task<bool> CanCreateProjectAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет, существует ли проект с таким названием у пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="title">Название проекта</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>True, если проект с таким названием уже существует</returns>
    Task<bool> ProjectTitleExistsAsync(Guid userId, string title, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает список всех проектов пользователя с краткой статистикой
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="includeArchived">Флаг включения архивных проектов</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список проектов пользователя</returns>
    Task<List<Project>> GetProjectsAsync(
        Guid userId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает детали проекта по идентификатору
    /// </summary>
    /// <param name="projectId">Идентификатор проекта</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Проект или null, если не найден</returns>
    Task<Project?> GetProjectByIdAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет проект
    /// </summary>
    /// <param name="projectId">Идентификатор проекта</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="title">Новое название проекта (опционально)</param>
    /// <param name="isArchived">Флаг архивации (опционально)</param>
    /// <param name="fsrsSettings">Настройки FSRS (опционально)</param>
    /// <param name="ttsSettings">Настройки TTS (опционально)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Обновленный проект</returns>
    Task<Project> UpdateProjectAsync(
        Guid projectId,
        Guid userId,
        string? title = null,
        bool? isArchived = null,
        JsonTypes.FsrsSettings? fsrsSettings = null,
        JsonTypes.TtsSettings? ttsSettings = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Безвозвратно удаляет проект и каскадно чистит связанные сущности
    /// </summary>
    /// <param name="projectId">Идентификатор проекта</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task DeleteProjectAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

