using Microsoft.EntityFrameworkCore;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Dtos.Community;

namespace VocabularyService.Services;

/// <summary>
/// Сервис для управления правами доступа
/// </summary>
public class EntitlementService : IEntitlementService
{
    private readonly VocabularyServiceContext _context;
    private readonly ILogger<EntitlementService> _logger;

    public EntitlementService(
        VocabularyServiceContext context,
        ILogger<EntitlementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Проверяет наличие права доступа к колоде
    /// </summary>
    public async Task<EntitlementDto> CheckEntitlementAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken = default)
    {
        // Check if deck is public (FREE access)
        var deck = await _context.Decks
            .FirstOrDefaultAsync(d => d.Id == deckId, cancellationToken);

        if (deck == null)
        {
            return new EntitlementDto
            {
                HasAccess = false,
                Source = "NONE"
            };
        }

        // If deck is public and free, everyone has access
        if (deck.IsPublic && deck.LicenseType == "FREE")
        {
            return new EntitlementDto
            {
                HasAccess = true,
                Source = "FREE",
                GrantedAt = null
            };
        }

        // Check entitlements
        var entitlement = await _context.UserEntitlements
            .Where(e => e.UserId == userId 
                && e.DeckId == deckId 
                && e.IsActive)
            .OrderByDescending(e => e.GrantedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (entitlement != null)
        {
            return new EntitlementDto
            {
                HasAccess = true,
                Source = entitlement.Source,
                GrantedAt = entitlement.GrantedAt
            };
        }

        return new EntitlementDto
        {
            HasAccess = false,
            Source = "NONE"
        };
    }

    /// <summary>
    /// Предоставляет право доступа
    /// </summary>
    public async Task GrantEntitlementAsync(
        Guid userId,
        Guid deckId,
        Guid? productId,
        string source,
        string? externalOrderId = null,
        CancellationToken cancellationToken = default)
    {
        // Check if entitlement already exists
        var existing = await _context.UserEntitlements
            .FirstOrDefaultAsync(e => e.UserId == userId 
                && e.DeckId == deckId 
                && e.IsActive, cancellationToken);

        if (existing != null)
        {
            // Update existing entitlement
            existing.Source = source;
            existing.ProductId = productId;
            existing.ExternalOrderId = externalOrderId;
            existing.GrantedAt = DateTime.UtcNow;
            existing.IsActive = true;
        }
        else
        {
            // Create new entitlement
            var entitlement = new UserEntitlement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeckId = deckId,
                ProductId = productId,
                Source = source,
                ExternalOrderId = externalOrderId,
                GrantedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.UserEntitlements.Add(entitlement);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Entitlement granted: User {UserId}, Deck {DeckId}, Source {Source}",
            userId, deckId, source);
    }

    /// <summary>
    /// Отзывает право доступа
    /// </summary>
    public async Task RevokeEntitlementAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken = default)
    {
        var entitlement = await _context.UserEntitlements
            .FirstOrDefaultAsync(e => e.UserId == userId 
                && e.DeckId == deckId 
                && e.IsActive, cancellationToken);

        if (entitlement != null)
        {
            entitlement.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Entitlement revoked: User {UserId}, Deck {DeckId}",
                userId, deckId);
        }
    }
}
