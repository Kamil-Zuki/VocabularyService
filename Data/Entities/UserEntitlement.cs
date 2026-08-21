using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class UserEntitlement
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? ProductId { get; set; }

    public Guid DeckId { get; set; }

    public string Source { get; set; } = null!;

    public string? ExternalOrderId { get; set; }

    public DateTime GrantedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual Deck Deck { get; set; } = null!;

    public virtual Product? Product { get; set; }
}
