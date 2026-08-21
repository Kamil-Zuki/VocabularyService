namespace VocabularyService.Helpers;

/// <summary>Приведение статуса из БД к контракту API/Reader (SAVED вместо legacy LINGQ/LEARNING).</summary>
public static class TermApiStatusFormatter
{
    public static string ToClientStatus(string dbStatus)
    {
        var x = dbStatus.Trim().ToUpperInvariant();
        return x switch
        {
            "LINGQ" or "LEARNING" => "SAVED",
            _ => x,
        };
    }
}
