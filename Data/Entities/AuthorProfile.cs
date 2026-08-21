using System;
using System.Collections.Generic;
using VocabularyService.Data.Entities.JsonTypes;

namespace VocabularyService.Data.Entities;

public partial class AuthorProfile
{
    public Guid UserId { get; set; }

    public string? DisplayName { get; set; }

    public string? Bio { get; set; }

    public SocialLinks? SocialLinks { get; set; }

    public List<string> Badges { get; set; }

    public AuthorStatsCache? StatsCache { get; set; }

    public DateTime UpdatedAt { get; set; }
}
