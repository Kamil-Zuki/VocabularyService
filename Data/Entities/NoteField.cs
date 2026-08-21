using System;

namespace VocabularyService.Data.Entities;

/// <summary>Field definition inside a note type (stable <see cref="FieldKey"/> for templates and JSON).</summary>
public partial class NoteField
{
    public Guid Id { get; set; }

    public Guid NoteTypeId { get; set; }

    /// <summary>Stable machine key, e.g. Expression, Word — used in {{FieldKey}} templates.</summary>
    public required string FieldKey { get; set; }

    public required string Label { get; set; }

    /// <summary>text, textarea, tags, image, audio, url</summary>
    public required string FieldType { get; set; }

    public int SortOrder { get; set; }

    public bool Required { get; set; }

    public bool Archived { get; set; }

    public string? ConfigJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual NoteType NoteType { get; set; } = null!;
}
