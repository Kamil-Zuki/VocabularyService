using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Dtos.Subscriptions;

namespace VocabularyService.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly VocabularyServiceContext _context;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        VocabularyServiceContext context,
        ILogger<SubscriptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SubscriptionListItemDto>> ListAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        return await _context.DeckSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Include(s => s.Deck)
            .OrderByDescending(s => s.SubscribedAt)
            .Select(s => new SubscriptionListItemDto
            {
                DeckId = s.DeckId,
                ProjectId = s.Deck.ProjectId,
                Title = s.Deck.Title,
                SubscribedAt = s.SubscribedAt,
                LastAccessedAt = s.LastAccessedAt,
                LastSyncedVersion = s.LastSyncedVersion ?? 0
            })
            .ToListAsync(ct);
    }

    public async Task<SubscriptionListItemDto> SubscribeAsync(
        Guid userId,
        Guid deckId,
        CancellationToken ct = default)
    {
        var deck = await _context.Decks
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == deckId, ct);

        if (deck is null)
        {
            throw new KeyNotFoundException($"Deck with id '{deckId}' was not found.");
        }

        if (!deck.IsPublic)
        {
            var hasAccess = deck.OwnerId == userId ||
                            await _context.UserEntitlements
                                .AsNoTracking()
                                .AnyAsync(e =>
                                    e.UserId == userId &&
                                    e.DeckId == deckId &&
                                    e.IsActive,
                                    ct);

            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("User does not have access to this deck.");
            }
        }

        var existing = await _context.DeckSubscriptions
            .SingleOrDefaultAsync(s => s.UserId == userId && s.DeckId == deckId, ct);

        if (existing is null)
        {
            existing = new DeckSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeckId = deckId,
                SubscribedAt = DateTime.UtcNow,
                LastSyncedVersion = 0,
                LastAccessedAt = DateTime.UtcNow
            };

            _context.DeckSubscriptions.Add(existing);
            await _context.SaveChangesAsync(ct);

            // reload deck navigation if needed for projection consistency
            deck = await _context.Decks
                .AsNoTracking()
                .SingleAsync(d => d.Id == deckId, ct);
        }

        return new SubscriptionListItemDto
        {
            DeckId = existing.DeckId,
            ProjectId = deck.ProjectId,
            Title = deck.Title,
            SubscribedAt = existing.SubscribedAt,
            LastAccessedAt = existing.LastAccessedAt,
            LastSyncedVersion = existing.LastSyncedVersion ?? 0
        };
    }

    public async Task UnsubscribeAsync(
        Guid userId,
        Guid deckId,
        CancellationToken ct = default)
    {
        var existing = await _context.DeckSubscriptions
            .SingleOrDefaultAsync(s => s.UserId == userId && s.DeckId == deckId, ct);

        if (existing is null)
        {
            return;
        }

        _context.DeckSubscriptions.Remove(existing);
        await _context.SaveChangesAsync(ct);
    }
}

