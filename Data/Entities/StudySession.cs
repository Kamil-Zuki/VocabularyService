using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class StudySession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ProjectId { get; set; }

    public Guid? DeckId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int CardsReviewed { get; set; }

    public int DurationSec { get; set; }

    public int NewLearned { get; set; }

    public string Status { get; set; } = "ACTIVE"; // ACTIVE, COMPLETED

    public virtual Project Project { get; set; } = null!;
}
