using System;
using System.Collections.Generic;

namespace VocabularyService.Data.Entities;

public partial class ProjectLemma
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Text { get; set; } = null!;

    public string? PosTag { get; set; }

    public string Status { get; set; } = null!;

    public Guid? MainCardId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Card? MainCard { get; set; }

    public virtual Project Project { get; set; } = null!;
}
