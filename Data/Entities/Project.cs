using System;
using System.Collections.Generic;
using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Data.Entities;

public partial class Project
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public string SourceLang { get; set; } = null!;

    public string TargetLang { get; set; } = null!;

    public FsrsSettings FsrsSettings { get; set; } = null!;

    public TtsSettings? TtsSettings { get; set; }

    public ProjectStats Stats { get; set; } = null!;

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Deck> Decks { get; set; } = new List<Deck>();

    public virtual ICollection<ProjectLemma> ProjectLemmas { get; set; } = new List<ProjectLemma>();

    public virtual ICollection<ProjectTerm> ProjectTerms { get; set; } = new List<ProjectTerm>();

    public virtual ICollection<UserTermStatus> UserTermStatuses { get; set; } = new List<UserTermStatus>();

    public virtual ICollection<StudySession> StudySessions { get; set; } = new List<StudySession>();

    public virtual ICollection<UserCardProgress> UserCardProgresses { get; set; } = new List<UserCardProgress>();

    public virtual ICollection<UserBookProgress> UserBookProgresses { get; set; } = new List<UserBookProgress>();

    public virtual ICollection<NoteType> NoteTypes { get; set; } = new List<NoteType>();
}
