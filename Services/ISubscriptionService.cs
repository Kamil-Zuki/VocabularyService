using VocabularyService.Dtos.Subscriptions;

namespace VocabularyService.Services;

public interface ISubscriptionService
{
    Task<IReadOnlyList<SubscriptionListItemDto>> ListAsync(Guid userId, CancellationToken ct = default);

    Task<SubscriptionListItemDto> SubscribeAsync(Guid userId, Guid deckId, CancellationToken ct = default);

    Task UnsubscribeAsync(Guid userId, Guid deckId, CancellationToken ct = default);
}

