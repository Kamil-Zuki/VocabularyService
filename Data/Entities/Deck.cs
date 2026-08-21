using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class Deck
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Guid? ParentDeckId { get; set; }

    public Guid OwnerId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? CoverImageUrl { get; set; }

    public bool IsPublic { get; set; }

    public string ContributionPolicy { get; set; } = null!;

    public string LicenseType { get; set; } = null!;

    public Guid? ForkedFromId { get; set; }

    public int CardCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();

    public virtual ICollection<DeckSubscription> DeckSubscriptions { get; set; } = new List<DeckSubscription>();

    public virtual ICollection<DeckVersion> DeckVersions { get; set; } = new List<DeckVersion>();

    public virtual ICollection<Deck> InverseParentDeck { get; set; } = new List<Deck>();

    public virtual Deck? ParentDeck { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<UserEntitlement> UserEntitlements { get; set; } = new List<UserEntitlement>();
}
