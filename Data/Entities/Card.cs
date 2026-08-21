using NpgsqlTypes;

namespace VocabularyService.Data.Entities;

public partial class Card
{
    public Guid Id { get; set; }

    public Guid DeckId { get; set; }

    public Guid CreatorId { get; set; }

    /// <summary>Canonical content lives on the linked note.</summary>
    public Guid NoteId { get; set; }

    /// <summary>Denormalized text for PostgreSQL full-text search.</summary>
    public string SearchDocument { get; set; } = string.Empty;

    /// <summary>Which card template (front/back) this study card uses.</summary>
    public Guid? CardTemplateId { get; set; }

    /// <summary>Опциональная связь с учебной единицей реальной формы (LingQ).</summary>
    public Guid? ProjectTermId { get; set; }

    public string? ExternalId { get; set; }

    public NpgsqlTsVector? SearchVector { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();

    public virtual Deck Deck { get; set; } = null!;

    public virtual ProjectTerm? ProjectTerm { get; set; }

    public virtual Note Note { get; set; } = null!;

    public virtual CardTemplate? CardTemplate { get; set; }

    public virtual ICollection<ProjectLemma> ProjectLemmas { get; set; } = new List<ProjectLemma>();

    public virtual ICollection<ReviewLog> ReviewLogs { get; set; } = new List<ReviewLog>();

    public virtual ICollection<UserCardProgress> UserCardProgresses { get; set; } = new List<UserCardProgress>();
}
