using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class ProductReview
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid UserId { get; set; }

    public short Rating { get; set; }

    public string? Comment { get; set; }

    public bool IsVerified { get; set; }

    public string? AuthorReply { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
