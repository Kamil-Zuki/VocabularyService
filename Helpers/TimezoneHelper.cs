namespace VocabularyService.Helpers;

/// <summary>
/// Вспомогательный класс для работы с часовыми поясами и RolloverHour
/// </summary>
public static class TimezoneHelper
{
    /// <summary>
    /// Получает начало дня для пользователя с учетом RolloverHour и часового пояса
    /// </summary>
    public static DateTime GetDayStart(DateTime utcNow, int rolloverHour, int? timezoneOffsetMinutes = null)
    {
        var userTime = timezoneOffsetMinutes.HasValue
            ? utcNow.AddMinutes(timezoneOffsetMinutes.Value)
            : utcNow;

        var dayStart = new DateTime(userTime.Year, userTime.Month, userTime.Day, rolloverHour, 0, 0, DateTimeKind.Unspecified);
        
        // Если текущее время меньше RolloverHour, день начался вчера
        if (userTime.Hour < rolloverHour)
        {
            dayStart = dayStart.AddDays(-1);
        }

        // Конвертируем обратно в UTC
        if (timezoneOffsetMinutes.HasValue)
        {
            dayStart = dayStart.AddMinutes(-timezoneOffsetMinutes.Value);
        }

        return dayStart.ToUniversalTime();
    }

    /// <summary>
    /// Получает конец дня для пользователя с учетом RolloverHour и часового пояса
    /// </summary>
    public static DateTime GetDayEnd(DateTime utcNow, int rolloverHour, int? timezoneOffsetMinutes = null)
    {
        var dayStart = GetDayStart(utcNow, rolloverHour, timezoneOffsetMinutes);
        return dayStart.AddDays(1);
    }

    /// <summary>
    /// Получает дату для пользователя с учетом RolloverHour
    /// </summary>
    public static DateOnly GetDateForUser(DateTime utcNow, int rolloverHour, int? timezoneOffsetMinutes = null)
    {
        var userTime = timezoneOffsetMinutes.HasValue
            ? utcNow.AddMinutes(timezoneOffsetMinutes.Value)
            : utcNow;

        var date = new DateOnly(userTime.Year, userTime.Month, userTime.Day);

        // Если текущее время меньше RolloverHour, это еще вчерашний день
        if (userTime.Hour < rolloverHour)
        {
            date = date.AddDays(-1);
        }

        return date;
    }

    /// <summary>
    /// Получает дату из DateTime с учетом RolloverHour
    /// </summary>
    public static DateOnly GetDateFromDateTime(DateTime dateTime, int rolloverHour, int? timezoneOffsetMinutes = null)
    {
        var userTime = timezoneOffsetMinutes.HasValue
            ? dateTime.AddMinutes(timezoneOffsetMinutes.Value)
            : dateTime;

        var date = new DateOnly(userTime.Year, userTime.Month, userTime.Day);

        if (userTime.Hour < rolloverHour)
        {
            date = date.AddDays(-1);
        }

        return date;
    }
}
