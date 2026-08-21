using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class DeckVersion
{
    public Guid Id { get; set; }

    public Guid DeckId { get; set; }

    public int VersionNumber { get; set; }

    public string ChangeDescription { get; set; } = null!;

    public Guid ModifiedByUserId { get; set; }

    public string SnapshotRef { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Deck Deck { get; set; } = null!;
}
