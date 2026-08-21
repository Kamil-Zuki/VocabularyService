using System;

namespace VocabularyService.Data.Entities;

/// <summary>
/// Учебная единиц проекта: точная форма слова или фраза (LingQ), не лемма.
/// </summary>
public partial class ProjectTerm
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    /// <summary>Отображаемый текст первого введения.</summary>
    public string Text { get; set; } = null!;

    /// <summary>Ключ совпадения: trim + lower + схлопывание пробелов.</summary>
    public string NormalizedText { get; set; } = null!;

    /// <summary>«WORD» или «PHRASE».</summary>
    public string Type { get; set; } = "WORD";

    public string? Language { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<UserTermStatus> UserTermStatuses { get; set; } = new List<UserTermStatus>();

    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
}
