using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class Product
{
    public Guid Id { get; set; }

    public Guid AuthorId { get; set; }

    public Guid LinkedDeckId { get; set; }

    public string Title { get; set; } = null!;

    public string? DescriptionHtml { get; set; }

    public string? CoverImageUrl { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = null!;

    public string Status { get; set; } = null!;

    public float AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public int SalesCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Deck LinkedDeck { get; set; } = null!;

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual ICollection<UserEntitlement> UserEntitlements { get; set; } = new List<UserEntitlement>();
}
