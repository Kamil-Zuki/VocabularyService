using System;
using System.Collections.Generic;
using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Data.Entities;

/// <summary>Anki-like note: owns dynamic field values; one or more study cards may reference it.</summary>
public partial class Note
{
    public Guid Id { get; set; }

    public Guid DeckId { get; set; }

    public Guid CreatorId { get; set; }

    public Guid NoteTypeId { get; set; }

    public Dictionary<string, NoteFieldValue> FieldValues { get; set; } = new();

    public Guid? ProjectTermId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Deck Deck { get; set; } = null!;

    public virtual NoteType NoteType { get; set; } = null!;

    public virtual ProjectTerm? ProjectTerm { get; set; }

    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
}
