namespace VocabularyService.Dtos.Study;

/// <summary>
/// DTO для состояния SRS карточки
/// </summary>
public class SrsStateDto
{
    public string State { get; set; } = "NEW"; // NEW, LEARNING, REVIEW, RELEARNING; MATURE — устар. в строке для статистики
    public int CurrentInterval { get; set; } // days
    /// <summary>FSRS learning/relearning step (0 when not in intraday steps).</summary>
    public int Step { get; set; }
    /// <summary>Next due (UTC). Useful for dev diagnostics of learning queue issues.</summary>
    public DateTime? DueUtc { get; set; }
}
