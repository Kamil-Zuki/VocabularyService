#nullable enable
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using VocabularyService.Data;
using VocabularyService.Data.Entities;

namespace VocabularyService.Services.Study;

public sealed class AnkiStudyQueueService : IAnkiStudyQueueService
{
    private readonly VocabularyServiceContext _context;
    private readonly IDatabase _redis;

    public AnkiStudyQueueService(VocabularyServiceContext context, IConnectionMultiplexer redis)
    {
        _context = context;
        _redis = redis.GetDatabase();
    }

    public async Task InitializeDueQueueAsync(
        Guid sessionId,
        IReadOnlyList<Guid> orderedCardIds,
        CancellationToken cancellationToken = default)
    {
        var dueKey = StudyQueueConstants.DueQueueKey(sessionId);
        var learningKey = StudyQueueConstants.LearningZsetKey(sessionId);

        await _redis.KeyDeleteAsync(dueKey).ConfigureAwait(false);
        await _redis.KeyDeleteAsync(learningKey).ConfigureAwait(false);

        if (orderedCardIds.Count == 0)
            return;

        var values = orderedCardIds.Select(id => (RedisValue)id.ToString()).ToArray();
        await _redis.ListRightPushAsync(dueKey, values).ConfigureAwait(false);
        await _redis.KeyExpireAsync(dueKey, StudyQueueConstants.SessionDataTtl).ConfigureAwait(false);
        await _redis.KeyExpireAsync(learningKey, StudyQueueConstants.SessionDataTtl).ConfigureAwait(false);

        // Drop stale legacy mirror; :due is the source of truth for new sessions.
        await _redis.KeyDeleteAsync(StudyQueueConstants.LegacyQueueKey(sessionId)).ConfigureAwait(false);
    }

    public async Task<Guid?> PopDueCardIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var dueKey = StudyQueueConstants.DueQueueKey(sessionId);
        var learningKey = StudyQueueConstants.LearningZsetKey(sessionId);
        var nowTicks = DateTime.UtcNow.Ticks;

        // 1) Timed learning/relearning due now (sorted by due time).
        var dueLearning = await _redis.SortedSetRangeByScoreAsync(
                learningKey,
                double.NegativeInfinity,
                nowTicks,
                exclude: Exclude.None,
                order: Order.Ascending,
                take: 1)
            .ConfigureAwait(false);

        if (dueLearning.Length > 0 && Guid.TryParse(dueLearning[0], out var learningCardId))
        {
            await _redis.SortedSetRemoveAsync(learningKey, dueLearning[0]).ConfigureAwait(false);
            return learningCardId;
        }

        // 2) Main due FIFO list.
        while (true)
        {
            var raw = await _redis.ListLeftPopAsync(dueKey).ConfigureAwait(false);
            if (raw.IsNullOrEmpty)
            {
                // Legacy fallback: drain old queue into due list once.
                var migrated = await MigrateLegacyQueueAsync(sessionId).ConfigureAwait(false);
                if (!migrated)
                    return null;

                raw = await _redis.ListLeftPopAsync(dueKey).ConfigureAwait(false);
                if (raw.IsNullOrEmpty)
                    return null;
            }

            if (!Guid.TryParse(raw, out var cardId))
                continue;

            await ScrubCardFromLegacyQueueAsync(sessionId, cardId).ConfigureAwait(false);
            return cardId;
        }
    }

    public async Task<bool> IsCardQueuedAsync(Guid sessionId, Guid cardId, CancellationToken cancellationToken = default)
    {
        var dueKey = StudyQueueConstants.DueQueueKey(sessionId);
        var learningKey = StudyQueueConstants.LearningZsetKey(sessionId);
        var id = cardId.ToString();

        var inLearning = await _redis.SortedSetScoreAsync(learningKey, id).ConfigureAwait(false);
        if (inLearning.HasValue)
            return true;

        var items = await _redis.ListRangeAsync(dueKey, 0, -1).ConfigureAwait(false);
        return items.Any(v => v == id);
    }

    public async Task EnqueueDueFrontAsync(Guid sessionId, Guid cardId, CancellationToken cancellationToken = default)
    {
        if (await IsCardQueuedAsync(sessionId, cardId, cancellationToken).ConfigureAwait(false))
            return;

        var dueKey = StudyQueueConstants.DueQueueKey(sessionId);
        await _redis.ListLeftPushAsync(dueKey, cardId.ToString()).ConfigureAwait(false);
        await _redis.KeyExpireAsync(dueKey, StudyQueueConstants.SessionDataTtl).ConfigureAwait(false);
    }

    public async Task EnqueueDueBackAsync(Guid sessionId, Guid cardId, CancellationToken cancellationToken = default)
    {
        if (await IsCardQueuedAsync(sessionId, cardId, cancellationToken).ConfigureAwait(false))
            return;

        var dueKey = StudyQueueConstants.DueQueueKey(sessionId);
        await _redis.ListRightPushAsync(dueKey, cardId.ToString()).ConfigureAwait(false);
        await _redis.KeyExpireAsync(dueKey, StudyQueueConstants.SessionDataTtl).ConfigureAwait(false);
    }

    public async Task ScheduleLearningAsync(Guid sessionId, Guid cardId, DateTime dueUtc, CancellationToken cancellationToken = default)
    {
        await RemoveFromQueuesAsync(sessionId, cardId, cancellationToken).ConfigureAwait(false);

        var learningKey = StudyQueueConstants.LearningZsetKey(sessionId);
        await _redis.SortedSetAddAsync(learningKey, cardId.ToString(), dueUtc.Ticks).ConfigureAwait(false);
        await _redis.KeyExpireAsync(learningKey, StudyQueueConstants.SessionDataTtl).ConfigureAwait(false);
    }

    public async Task RemoveFromQueuesAsync(Guid sessionId, Guid cardId, CancellationToken cancellationToken = default)
    {
        var dueKey = StudyQueueConstants.DueQueueKey(sessionId);
        var learningKey = StudyQueueConstants.LearningZsetKey(sessionId);
        var id = cardId.ToString();

        await _redis.SortedSetRemoveAsync(learningKey, id).ConfigureAwait(false);

        var items = await _redis.ListRangeAsync(dueKey, 0, -1).ConfigureAwait(false);
        if (items.Length == 0)
            return;

        var filtered = items.Where(v => v != id).Select(v => v.ToString()).ToArray();
        await _redis.KeyDeleteAsync(dueKey).ConfigureAwait(false);
        if (filtered.Length > 0)
        {
            await _redis.ListRightPushAsync(dueKey, filtered.Select(v => (RedisValue)v).ToArray()).ConfigureAwait(false);
            await _redis.KeyExpireAsync(dueKey, StudyQueueConstants.SessionDataTtl).ConfigureAwait(false);
        }
    }

    public async Task<Guid?> FindLearnAheadCardIdAsync(
        StudySession session,
        CancellationToken cancellationToken = default)
    {
        var deckIds = await ResolveDeckIdsAsync(session, cancellationToken).ConfigureAwait(false);
        if (deckIds.Count == 0)
            return null;

        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(StudyQueueConstants.LearnAheadLimitMinutes);

        var cardId = await _context.UserCardProgresses
            .Where(p => p.UserId == session.UserId
                && p.ProjectId == session.ProjectId
                && deckIds.Contains(p.Card.DeckId)
                && (p.State == 1 || p.State == 3)
                && p.Due > now
                && p.Due <= cutoff
                && !p.IsSuspended)
            .OrderBy(p => p.Due)
            .Select(p => p.CardId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return cardId != default ? cardId : null;
    }

    private async Task<bool> MigrateLegacyQueueAsync(Guid sessionId)
    {
        var legacyKey = StudyQueueConstants.LegacyQueueKey(sessionId);
        var dueKey = StudyQueueConstants.DueQueueKey(sessionId);

        var legacyItems = await _redis.ListRangeAsync(legacyKey, 0, -1).ConfigureAwait(false);
        if (legacyItems.Length == 0)
            return false;

        var dueLen = await _redis.ListLengthAsync(dueKey).ConfigureAwait(false);
        if (dueLen > 0)
            return false;

        var dueExists = await _redis.KeyExistsAsync(dueKey).ConfigureAwait(false);
        if (dueExists)
        {
            // :due was initialized and drained; stale legacy mirror must not refill the queue.
            await _redis.KeyDeleteAsync(legacyKey).ConfigureAwait(false);
            return false;
        }

        var unique = legacyItems
            .Select(v => v.ToString())
            .Where(id => Guid.TryParse(id, out _))
            .Distinct(StringComparer.Ordinal)
            .Select(id => (RedisValue)id)
            .ToArray();

        await _redis.KeyDeleteAsync(legacyKey).ConfigureAwait(false);

        if (unique.Length == 0)
            return false;

        await _redis.ListRightPushAsync(dueKey, unique).ConfigureAwait(false);
        await _redis.KeyExpireAsync(dueKey, StudyQueueConstants.SessionDataTtl).ConfigureAwait(false);
        return true;
    }

    private async Task ScrubCardFromLegacyQueueAsync(Guid sessionId, Guid cardId)
    {
        var legacyKey = StudyQueueConstants.LegacyQueueKey(sessionId);
        var items = await _redis.ListRangeAsync(legacyKey, 0, -1).ConfigureAwait(false);
        if (items.Length == 0)
            return;

        var id = cardId.ToString();
        if (!items.Any(v => v == id))
            return;

        var filtered = items.Where(v => v != id).Select(v => v.ToString()).ToArray();
        await _redis.KeyDeleteAsync(legacyKey).ConfigureAwait(false);
        if (filtered.Length > 0)
        {
            await _redis.ListRightPushAsync(
                legacyKey,
                filtered.Select(v => (RedisValue)v).ToArray()).ConfigureAwait(false);
        }
    }

    private async Task<List<Guid>> ResolveDeckIdsAsync(StudySession session, CancellationToken cancellationToken)
    {
        if (session.DeckId.HasValue)
        {
            return await CollectDeckIdsRecursiveAsync(session.DeckId.Value, session.UserId, cancellationToken)
                .ConfigureAwait(false);
        }

        return await _context.Decks
            .Where(d => d.ProjectId == session.ProjectId)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<List<Guid>> CollectDeckIdsRecursiveAsync(
        Guid deckId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = new List<Guid> { deckId };
        var childIds = await _context.Decks
            .Where(d => d.ParentDeckId == deckId && d.OwnerId == userId)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var childId in childIds)
        {
            result.AddRange(await CollectDeckIdsRecursiveAsync(childId, userId, cancellationToken).ConfigureAwait(false));
        }

        return result;
    }
}
