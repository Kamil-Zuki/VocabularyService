using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class ReviewLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CardId { get; set; }

    public Guid SessionId { get; set; }

    public short Rating { get; set; }

    public short StateBefore { get; set; }

    public short StateAfter { get; set; }

    public int StepBefore { get; set; }

    public int StepAfter { get; set; }

    public int RepsBefore { get; set; }

    public int RepsAfter { get; set; }

    public int LapsesBefore { get; set; }

    public int LapsesAfter { get; set; }

    public int ElapsedDaysBefore { get; set; }

    public int ElapsedDaysAfter { get; set; }

    public int ScheduledDaysBefore { get; set; }

    public int ScheduledDaysAfter { get; set; }

    public DateTime LastReviewBefore { get; set; }

    public DateTime LastReviewAfter { get; set; }

    public DateTime DueBefore { get; set; }

    public DateTime DueAfter { get; set; }

    public float StabilityBefore { get; set; }

    public float StabilityAfter { get; set; }

    public float DifficultyBefore { get; set; }

    public float DifficultyAfter { get; set; }

    public int ReviewDurationMs { get; set; }

    public string? UserAnswer { get; set; }

    public string? AnswerValidationResult { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Card Card { get; set; } = null!;
}
