using System;
using System.Collections.Generic;
using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Data.Entities;

public partial class Contribution
{
    public Guid Id { get; set; }

    public Guid TargetDeckId { get; set; }

    public Guid? TargetCardId { get; set; }

    public Guid AuthorId { get; set; }

    public string Type { get; set; } = null!;

    public ContributionPayload Payload { get; set; } = null!;

    public string? Comment { get; set; }

    public string Status { get; set; } = null!;

    public Guid? ReviewerId { get; set; }

    public string? ResolutionComment { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Card? TargetCard { get; set; }

    public virtual Deck TargetDeck { get; set; } = null!;
}
