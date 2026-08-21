using VocabularyService.Data.Entities;
using VocabularyService.Dtos;

namespace VocabularyService.Services;

/// <summary>
/// Интерфейс сервиса для работы с настройками пользователя
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Получает настройки пользователя. Если они отсутствуют, создает их с дефолтными значениями.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Настройки пользователя</returns>
    Task<UserSetting> GetUserSettingsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет настройки пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="dto">DTO с данными для обновления</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Обновленные настройки пользователя</returns>
    Task<UserSetting> UpdateUserSettingsAsync(Guid userId, UpdateUserSettingsDto dto, CancellationToken cancellationToken = default);
}
