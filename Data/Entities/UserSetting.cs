using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class UserSetting
{
    public Guid UserId { get; set; }

    public int RolloverHour { get; set; }

    public int CurrentStreak { get; set; }

    public int MaxStreak { get; set; }

    public DateOnly? LastStudyDate { get; set; }

    public int DailyGoalNew { get; set; }

    public int DailyGoalReview { get; set; }

    public string InterfaceLanguage { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}
