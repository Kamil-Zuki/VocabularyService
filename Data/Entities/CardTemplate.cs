using System;

namespace VocabularyService.Data.Entities;

public partial class CardTemplate
{
    public Guid Id { get; set; }

    public Guid NoteTypeId { get; set; }

    public required string TemplateKey { get; set; }

    public required string Name { get; set; }

    public required string FrontTemplate { get; set; }

    public required string BackTemplate { get; set; }

    public string TargetSkill { get; set; } = "Reading";

    public int SortOrder { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual NoteType NoteType { get; set; } = null!;
}
