using System;

namespace VocabularyService.Data.Entities;

/// <summary>
/// Статус термина для конкретного пользователя в рамках проекта.
/// </summary>
public partial class UserTermStatus
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ProjectId { get; set; }

    public Guid ProjectTermId { get; set; }

    /// <summary>NEW, SAVED (сохранённый термин с переводом; в старых данных могло быть LINGQ), KNOWN, IGNORED</summary>
    public string Status { get; set; } = "NEW";

    public int ReadingLevel { get; set; } = 0;
    public int ListeningLevel { get; set; } = 0;
    public int WritingLevel { get; set; } = 0;
    public int SpeakingLevel { get; set; } = 0;

    public string? Meaning { get; set; }

    public string? FirstSentence { get; set; }

    public string? FirstSourceTitle { get; set; }

    public string? FirstSourceUrl { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ProjectTerm ProjectTerm { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;
}
