using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class UserCardProgress
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CardId { get; set; }

    public Guid ProjectId { get; set; }

    public short State { get; set; }

    /// <summary>Шаг в Learning/Relearning (FSRS).</summary>
    public int Step { get; set; }

    public float Stability { get; set; }

    public float Difficulty { get; set; }

    public DateTime Due { get; set; }

    public int ElapsedDays { get; set; }

    public int ScheduledDays { get; set; }

    public int Reps { get; set; }

    public int Lapses { get; set; }

    public bool IsSuspended { get; set; }

    public DateTime LastReview { get; set; }

    public virtual Card Card { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;
}
