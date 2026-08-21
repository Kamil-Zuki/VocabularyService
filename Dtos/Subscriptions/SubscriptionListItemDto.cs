namespace VocabularyService.Dtos.Subscriptions;

public class SubscriptionListItemDto
{
    public Guid DeckId { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime SubscribedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int LastSyncedVersion { get; set; }
}
