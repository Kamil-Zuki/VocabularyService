using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class DeletedObject
{
    public Guid Id { get; set; }

    public Guid EntityId { get; set; }

    public string EntityType { get; set; } = null!;

    public Guid UserId { get; set; }

    public Guid? ParentId { get; set; }

    public DateTime DeletedAt { get; set; }
}
