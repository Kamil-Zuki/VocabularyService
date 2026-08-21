#nullable enable
using VocabularyService.Data.Entities;

namespace VocabularyService.Services.Study;

public sealed record StudyQueuePopResult(Guid CardId, bool FromLearnAhead);

public interface IAnkiStudyQueueService
{
    Task InitializeDueQueueAsync(Guid sessionId, IReadOnlyList<Guid> orderedCardIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pops the next card id from due list or timed learning zset. Returns null when empty.
    /// </summary>
    Task<Guid?> PopDueCardIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<bool> IsCardQueuedAsync(Guid sessionId, Guid cardId, CancellationToken cancellationToken = default);

    Task EnqueueDueFrontAsync(Guid sessionId, Guid cardId, CancellationToken cancellationToken = default);

    Task EnqueueDueBackAsync(Guid sessionId, Guid cardId, CancellationToken cancellationToken = default);

    Task ScheduleLearningAsync(Guid sessionId, Guid cardId, DateTime dueUtc, CancellationToken cancellationToken = default);

    Task RemoveFromQueuesAsync(Guid sessionId, Guid cardId, CancellationToken cancellationToken = default);

    Task<Guid?> FindLearnAheadCardIdAsync(
        StudySession session,
        CancellationToken cancellationToken = default);
}
