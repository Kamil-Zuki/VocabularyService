using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

/// <summary>Anki-like note type: groups field definitions and card templates per project.</summary>
public partial class NoteType
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public required string Name { get; set; }

    public int Version { get; set; }

    public string? Css { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<NoteField> NoteFields { get; set; } = new List<NoteField>();

    public virtual ICollection<CardTemplate> CardTemplates { get; set; } = new List<CardTemplate>();

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
}
