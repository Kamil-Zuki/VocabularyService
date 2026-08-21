namespace VocabularyService.Services;

/// <summary>Строка списка терминов (до маппинга в gRPC).</summary>
public sealed record ProjectTermListRow(
    Guid TermId,
    string Text,
    string NormalizedText,
    string Type,
    string Language,
    string DbStatus,
    string? Meaning,
    string? FirstSentence,
    string? FirstSourceTitle,
    string? FirstSourceUrl,
    DateTime UpdatedAtUtc,
    int RelatedCardCount = 0,
    int ReadingLevel = 0,
    int ListeningLevel = 0,
    int WritingLevel = 0,
    int SpeakingLevel = 0);
