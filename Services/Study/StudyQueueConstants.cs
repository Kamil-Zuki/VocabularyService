namespace VocabularyService.Services.Study;

internal static class StudyQueueConstants
{
    public const int LearnAheadLimitMinutes = 20;
    public const int MaxLearningDeferAttempts = 64;
    public static readonly TimeSpan SessionDataTtl = TimeSpan.FromHours(24);

    public static string DueQueueKey(Guid sessionId) => $"study:session:{sessionId}:due";
    public static string LearningZsetKey(Guid sessionId) => $"study:session:{sessionId}:learning";
    public static string SeenTermsKey(Guid sessionId) => $"study:session:{sessionId}:seen_terms";
    public static string SeenTermCardsKey(Guid sessionId) => $"study:session:{sessionId}:seen_term_cards";

    /// <summary>Legacy list key; migrated on read when due queue is empty.</summary>
    public static string LegacyQueueKey(Guid sessionId) => $"study:session:{sessionId}:queue";
    public static string LegacySeenLemmasKey(Guid sessionId) => $"study:session:{sessionId}:seen_lemmas";
    public static string LegacySeenLemmaCardsKey(Guid sessionId) => $"study:session:{sessionId}:seen_lemma_cards";
}
