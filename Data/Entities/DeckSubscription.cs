using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class DeckSubscription
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid DeckId { get; set; }

    public int? LastSyncedVersion { get; set; }

    public DateTime SubscribedAt { get; set; }

    public DateTime LastAccessedAt { get; set; }

    public virtual Deck Deck { get; set; } = null!;
}
