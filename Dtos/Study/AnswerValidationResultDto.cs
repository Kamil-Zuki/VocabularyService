namespace VocabularyService.Dtos.Study;

/// <summary>
/// Результат проверки ответа пользователя
/// </summary>
public class AnswerValidationResultDto
{
    /// <summary>
    /// Точное совпадение (case-insensitive)
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Совпадение с учетом опечаток (Fuzzy Matching)
    /// </summary>
    public bool IsFuzzyMatch { get; set; }

    /// <summary>
    /// Какой синоним совпал (если применимо)
    /// </summary>
    public string? MatchedSynonym { get; set; }

    /// <summary>
    /// Процент схожести (0-1)
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// Предложение правильного ответа (если не совпало)
    /// </summary>
    public string? Suggestion { get; set; }
}
