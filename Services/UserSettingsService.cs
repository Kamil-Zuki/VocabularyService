using Microsoft.EntityFrameworkCore;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Dtos;

namespace VocabularyService.Services;

/// <summary>
/// Сервис для работы с настройками пользователя
/// </summary>
public class UserSettingsService : IUserSettingsService
{
    private readonly VocabularyServiceContext _context;
    private readonly ILogger<UserSettingsService> _logger;

    public UserSettingsService(
        VocabularyServiceContext context,
        ILogger<UserSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Получает настройки пользователя. Если они отсутствуют, создает их с дефолтными значениями (Lazy Initialization).
    /// </summary>
    public async Task<UserSetting> GetUserSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (settings == null)
        {
            _logger.LogInformation("Settings not found for user {UserId}. Initializing with default values.", userId);

            settings = new UserSetting
            {
                UserId = userId,
                RolloverHour = 4,
                DailyGoalNew = 20,
                DailyGoalReview = 100,
                InterfaceLanguage = "en",
                CurrentStreak = 0,
                MaxStreak = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }

    /// <summary>
    /// Обновляет настройки пользователя
    /// </summary>
    public async Task<UserSetting> UpdateUserSettingsAsync(Guid userId, UpdateUserSettingsDto dto, CancellationToken cancellationToken = default)
    {
        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (settings == null)
        {
            throw new KeyNotFoundException($"Settings for user {userId} not found. Please call GetUserSettings first to initialize them.");
        }

        // Валидация
        if (dto.RolloverHour.HasValue && (dto.RolloverHour.Value < 0 || dto.RolloverHour.Value > 23))
        {
            throw new ArgumentException("RolloverHour must be between 0 and 23");
        }

        if (dto.DailyGoalNew.HasValue && dto.DailyGoalNew.Value <= 0)
        {
            throw new ArgumentException("DailyGoalNew must be greater than 0");
        }

        if (dto.DailyGoalReview.HasValue && dto.DailyGoalReview.Value <= 0)
        {
            throw new ArgumentException("DailyGoalReview must be greater than 0");
        }

        // Обновление только переданных полей
        if (dto.RolloverHour.HasValue)
        {
            settings.RolloverHour = dto.RolloverHour.Value;
        }

        if (dto.DailyGoalNew.HasValue)
        {
            settings.DailyGoalNew = dto.DailyGoalNew.Value;
        }

        if (dto.DailyGoalReview.HasValue)
        {
            settings.DailyGoalReview = dto.DailyGoalReview.Value;
        }

        if (!string.IsNullOrEmpty(dto.InterfaceLanguage))
        {
            settings.InterfaceLanguage = dto.InterfaceLanguage;
        }

        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated settings for user {UserId}", userId);

        return settings;
    }
}
