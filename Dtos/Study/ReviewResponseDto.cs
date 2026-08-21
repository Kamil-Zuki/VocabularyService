namespace VocabularyService.Dtos.Study;

/// <summary>
/// DTO для ответа после отправки оценки
/// </summary>
public class ReviewResponseDto
{
    public Guid CardId { get; set; }
    public DateTime NextReviewDate { get; set; }
    public string Interval { get; set; } = string.Empty; // e.g., "3d", "2w"
    public string State { get; set; } = "NEW"; // NEW, LEARNING, REVIEW, RELEARNING
    public double Stability { get; set; }
    public bool IsLeech { get; set; }
    public int BuriedSiblingsCount { get; set; }
    public AnswerValidationResultDto? AnswerValidation { get; set; } // Результат проверки ответа (если user_answer был предоставлен)
}
