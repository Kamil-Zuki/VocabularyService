namespace VocabularyService.Services.Study;

internal static class StudyIntervalFormatter
{
    public static string FormatUntilDue(DateTime due, DateTime from)
    {
        var diff = due - from;
        if (diff.TotalDays >= 1)
            return FormatDays((int)Math.Round(diff.TotalDays));

        if (diff.TotalMinutes >= 1)
            return $"{(int)Math.Round(diff.TotalMinutes)}m";

        return "1m";
    }

    private static string FormatDays(int days)
    {
        if (days < 1) return "0d";
        if (days < 7) return $"{days}d";
        if (days < 30) return $"{days / 7}w";
        if (days < 365) return $"{days / 30}mo";
        return $"{days / 365}y";
    }
}
