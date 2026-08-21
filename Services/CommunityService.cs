using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Domain;
using VocabularyService.Dtos.Cards;
using VocabularyService.Dtos.Community;
using VocabularyService.Helpers;

namespace VocabularyService.Services;

/// <summary>
/// Сервис для коллаборации и маркетплейса
/// </summary>
public class CommunityService : ICommunityService
{
    private readonly VocabularyServiceContext _context;
    private readonly ILogger<CommunityService> _logger;
    private readonly IEntitlementService _entitlementService;
    private readonly ICardService _cardService;

    public CommunityService(
        VocabularyServiceContext context,
        ILogger<CommunityService> logger,
        IEntitlementService entitlementService,
        ICardService cardService)
    {
        _context = context;
        _logger = logger;
        _entitlementService = entitlementService;
        _cardService = cardService;
    }

    // ============================================================================
    // Contributions (SR-COL-01 до SR-COL-08)
    // ============================================================================

    /// <summary>
    /// Создает предложение (SR-COL-01)
    /// </summary>
    public async Task<ContributionDto> CreateContributionAsync(
        Guid userId,
        CreateContributionDto request,
        CancellationToken cancellationToken = default)
    {
        // Verify deck exists
        var deck = await _context.Decks
            .FirstOrDefaultAsync(d => d.Id == request.DeckId, cancellationToken);

        if (deck == null)
        {
            throw new KeyNotFoundException($"Deck {request.DeckId} not found");
        }

        // Check contribution policy (SR-COL-06)
        if (deck.ContributionPolicy == "CLOSED")
        {
            throw new UnauthorizedAccessException("Deck does not accept contributions");
        }

        if (deck.ContributionPolicy == "RESTRICTED")
        {
            // Check if user has subscription or entitlement
            var hasAccess = await _entitlementService.CheckEntitlementAsync(userId, request.DeckId, cancellationToken);
            if (!hasAccess.HasAccess)
            {
                // Check subscription
                var hasSubscription = await _context.DeckSubscriptions
                    .AnyAsync(s => s.UserId == userId && s.DeckId == request.DeckId, cancellationToken);

                if (!hasSubscription)
                {
                    throw new UnauthorizedAccessException("Deck contributions are restricted to subscribers");
                }
            }
        }

        // Validate type
        if (request.Type == "EDIT" || request.Type == "DELETE")
        {
            if (!request.CardId.HasValue)
            {
                throw new ArgumentException("Card ID is required for EDIT and DELETE types");
            }

            var cardExists = await _context.Cards
                .AnyAsync(c => c.Id == request.CardId.Value && c.DeckId == request.DeckId, cancellationToken);

            if (!cardExists)
            {
                throw new KeyNotFoundException($"Card {request.CardId} not found in deck {request.DeckId}");
            }
        }

        // Create contribution
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            TargetDeckId = request.DeckId,
            TargetCardId = request.CardId,
            AuthorId = userId,
            Type = request.Type,
            Payload = request.Content,
            Comment = request.Comment,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Contributions.Add(contribution);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Contribution created: {ContributionId}, Type: {Type}, Deck: {DeckId}",
            contribution.Id, request.Type, request.DeckId);

        return MapToContributionDto(contribution);
    }

    /// <summary>
    /// Получает список предложений (SR-COL-03)
    /// </summary>
    public async Task<List<ContributionDto>> GetContributionsAsync(
        Guid userId,
        Guid? deckId,
        string? status,
        string role, // AUTHOR or MODERATOR
        CancellationToken cancellationToken = default)
    {
        var query = _context.Contributions.AsQueryable();

        if (role == "AUTHOR")
        {
            // User's own contributions
            query = query.Where(c => c.AuthorId == userId);
        }
        else if (role == "MODERATOR")
        {
            // Contributions to decks owned by user
            query = query.Where(c => c.TargetDeck.OwnerId == userId);
        }
        else
        {
            throw new ArgumentException("Role must be AUTHOR or MODERATOR");
        }

        if (deckId.HasValue)
        {
            query = query.Where(c => c.TargetDeckId == deckId.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(c => c.Status == status);
        }

        var contributions = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return contributions.Select(MapToContributionDto).ToList();
    }

    /// <summary>
    /// Получает предложение с Diff (SR-COL-03)
    /// </summary>
    public async Task<(ContributionDto Contribution, ContributionDiffDto Diff)> GetContributionAsync(
        Guid userId,
        Guid contributionId,
        CancellationToken cancellationToken = default)
    {
        var contribution = await _context.Contributions
            .Include(c => c.TargetCard!)
                .ThenInclude(card => card!.Note)
            .Include(c => c.TargetDeck)
            .FirstOrDefaultAsync(c => c.Id == contributionId, cancellationToken);

        if (contribution == null)
        {
            throw new KeyNotFoundException($"Contribution {contributionId} not found");
        }

        // Check access: author or deck owner
        if (contribution.AuthorId != userId && contribution.TargetDeck.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        var contributionDto = MapToContributionDto(contribution);

        // Calculate diff
        var diff = new ContributionDiffDto
        {
            ProposedCard = contribution.Payload,
            ChangedFields = new List<string>(),
            IsConflict = false
        };

        if (contribution.Type == "EDIT" && contribution.TargetCard?.Note != null)
        {
            diff.OriginalCard = new ContributionPayload
            {
                FieldValues = CloneFieldMap(contribution.TargetCard.Note.FieldValues),
            };

            var proposed = contribution.Payload.FieldValues ?? new Dictionary<string, NoteFieldValue>();
            var origJson = JsonSerializer.Serialize(
                diff.OriginalCard.FieldValues.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value));
            var propJson = JsonSerializer.Serialize(
                proposed.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value));
            if (origJson != propJson)
                diff.ChangedFields.Add("field_values");

            if (contribution.TargetCard.UpdatedAt > contribution.CreatedAt)
                diff.IsConflict = true;
        }

        return (contributionDto, diff);
    }

    /// <summary>
    /// Принимает или отклоняет предложение (SR-COL-04)
    /// </summary>
    public async Task<(bool Success, Guid? MergedCardId)> ResolveContributionAsync(
        Guid userId,
        Guid contributionId,
        ResolveContributionDto request,
        CancellationToken cancellationToken = default)
    {
        var contribution = await _context.Contributions
            .Include(c => c.TargetCard)
            .Include(c => c.TargetDeck)
            .FirstOrDefaultAsync(c => c.Id == contributionId, cancellationToken);

        if (contribution == null)
        {
            throw new KeyNotFoundException($"Contribution {contributionId} not found");
        }

        // Check ownership
        if (contribution.TargetDeck.OwnerId != userId)
        {
            throw new UnauthorizedAccessException("Only deck owner can resolve contributions");
        }

        // Check status
        if (contribution.Status != "PENDING")
        {
            throw new InvalidOperationException($"Contribution is already {contribution.Status}");
        }

        Guid? mergedCardId = null;

        // Use transaction for atomic merge (SR-COL-04)
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (request.Status == "MERGED")
            {
                if (contribution.Type == "EDIT" && contribution.TargetCard != null)
                {
                    await _cardService.UpdateCardAsync(
                        contribution.TargetCard.Id,
                        userId,
                        new UpdateCardDto
                        {
                            FieldValues = NoteFieldMapHelper.NormalizeSentenceMiningMap(
                                contribution.Payload.FieldValues ?? new Dictionary<string, NoteFieldValue>()),
                        },
                        cancellationToken);
                    mergedCardId = contribution.TargetCard.Id;
                }
                else if (contribution.Type == "ADD")
                {
                    var created = await _cardService.CreateCardAsDeckOwnerAsync(
                        userId,
                        new CreateCardDto
                        {
                            UserId = contribution.AuthorId,
                            DeckId = contribution.TargetDeckId,
                            FieldValues = NoteFieldMapHelper.NormalizeSentenceMiningMap(
                                contribution.Payload.FieldValues ?? new Dictionary<string, NoteFieldValue>()),
                        },
                        cancellationToken);
                    mergedCardId = created.Id;
                }
                else if (contribution.Type == "DELETE" && contribution.TargetCard != null)
                {
                    await _cardService.DeleteCardAsync(contribution.TargetCard.Id, userId, cancellationToken);
                }

                // Update contribution status
                contribution.Status = "MERGED";
                contribution.ReviewerId = userId;
                contribution.ResolutionComment = request.ResolutionComment;
                contribution.UpdatedAt = DateTime.UtcNow;

                // Update deck version (for sync)
                contribution.TargetDeck.UpdatedAt = DateTime.UtcNow;

                // Grant entitlement to contributor (SR-COL-07)
                await _entitlementService.GrantEntitlementAsync(
                    contribution.AuthorId,
                    contribution.TargetDeckId,
                    null,
                    "CONTRIBUTION",
                    null,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Contribution {ContributionId} merged by {UserId}",
                    contributionId, userId);
            }
            else if (request.Status == "REJECTED")
            {
                contribution.Status = "REJECTED";
                contribution.ReviewerId = userId;
                contribution.ResolutionComment = request.ResolutionComment;
                contribution.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Contribution {ContributionId} rejected by {UserId}",
                    contributionId, userId);
            }
            else
            {
                throw new ArgumentException($"Invalid status: {request.Status}");
            }
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return (true, mergedCardId);
    }

    /// <summary>
    /// Обновляет политику вкладов (SR-COL-06)
    /// </summary>
    public async Task<bool> UpdateContributionPolicyAsync(
        Guid userId,
        Guid deckId,
        string policy, // OPEN, RESTRICTED, CLOSED
        CancellationToken cancellationToken = default)
    {
        if (policy != "OPEN" && policy != "RESTRICTED" && policy != "CLOSED")
        {
            throw new ArgumentException("Policy must be OPEN, RESTRICTED, or CLOSED");
        }

        var deck = await _context.Decks
            .FirstOrDefaultAsync(d => d.Id == deckId && d.OwnerId == userId, cancellationToken);

        if (deck == null)
        {
            throw new KeyNotFoundException($"Deck {deckId} not found or access denied");
        }

        deck.ContributionPolicy = policy;
        deck.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Contribution policy updated for deck {DeckId}: {Policy}",
            deckId, policy);

        return true;
    }

    // ============================================================================
    // Publishing (SR-PUB-01 до SR-PUB-04)
    // ============================================================================

    /// <summary>
    /// Публикует колоду (SR-PUB-01)
    /// </summary>
    public async Task<bool> PublishDeckAsync(
        Guid userId,
        PublishDeckDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.LicenseType != "FREE" && request.LicenseType != "COMMERCIAL")
        {
            throw new ArgumentException("LicenseType must be FREE or COMMERCIAL");
        }

        var deck = await _context.Decks
            .FirstOrDefaultAsync(d => d.Id == request.DeckId && d.OwnerId == userId, cancellationToken);

        if (deck == null)
        {
            throw new KeyNotFoundException($"Deck {request.DeckId} not found or access denied");
        }

        // Check if deck is a fork of commercial content (SR-MKT-04)
        if (deck.ForkedFromId.HasValue)
        {
            var originalDeck = await _context.Decks
                .FirstOrDefaultAsync(d => d.Id == deck.ForkedFromId.Value, cancellationToken);

            if (originalDeck != null && originalDeck.LicenseType == "COMMERCIAL")
            {
                throw new InvalidOperationException("Cannot publish a fork of commercial content");
            }
        }

        deck.IsPublic = true;
        deck.LicenseType = request.LicenseType;
        deck.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deck {DeckId} published with license {LicenseType}",
            request.DeckId, request.LicenseType);

        return true;
    }

    /// <summary>
    /// Клонирует колоду (SR-PUB-02)
    /// </summary>
    public async Task<Guid> ForkDeckAsync(
        Guid userId,
        ForkDeckDto request,
        CancellationToken cancellationToken = default)
    {
        // Verify target project belongs to user
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.TargetProjectId && p.UserId == userId, cancellationToken);

        if (project == null)
        {
            throw new KeyNotFoundException($"Project {request.TargetProjectId} not found or access denied");
        }

        // Verify source deck exists and is accessible
        var sourceDeck = await _context.Decks
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.Id == request.DeckId, cancellationToken);

        if (sourceDeck == null)
        {
            throw new KeyNotFoundException($"Deck {request.DeckId} not found");
        }

        // Check access: public or has entitlement
        if (!sourceDeck.IsPublic)
        {
            var entitlement = await _entitlementService.CheckEntitlementAsync(userId, request.DeckId, cancellationToken);
            if (!entitlement.HasAccess)
            {
                throw new UnauthorizedAccessException("Deck is not public and you don't have access");
            }
        }

        // Determine license type (SR-MKT-04)
        string licenseType;
        if (sourceDeck.LicenseType == "COMMERCIAL")
        {
            licenseType = "COMMERCIAL_DERIVATIVE";
        }
        else
        {
            licenseType = "PRIVATE";
        }

        // Create new deck
        var forkedDeck = new Deck
        {
            Id = Guid.NewGuid(),
            ProjectId = request.TargetProjectId,
            OwnerId = userId,
            Title = request.NewTitle ?? $"{sourceDeck.Title} (Copy)",
            Description = sourceDeck.Description,
            CoverImageUrl = sourceDeck.CoverImageUrl,
            IsPublic = false,
            ContributionPolicy = "CLOSED",
            LicenseType = licenseType,
            ForkedFromId = sourceDeck.Id,
            CardCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Decks.Add(forkedDeck);

        // Copy all cards (notes + search_document)
        var sourceCards = await _context.Cards
            .Include(c => c.Note)
            .Where(c => c.DeckId == sourceDeck.Id)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var sourceCard in sourceCards)
        {
            if (sourceCard.Note == null)
                continue;

            var newNoteId = Guid.NewGuid();
            var fv = CloneFieldMap(sourceCard.Note.FieldValues);
            var note = new Note
            {
                Id = newNoteId,
                DeckId = forkedDeck.Id,
                CreatorId = userId,
                NoteTypeId = sourceCard.Note.NoteTypeId,
                FieldValues = fv,
                ProjectTermId = sourceCard.Note.ProjectTermId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Notes.Add(note);

            var forkedCard = new Card
            {
                Id = Guid.NewGuid(),
                DeckId = forkedDeck.Id,
                CreatorId = userId,
                NoteId = newNoteId,
                SearchDocument = NoteFieldMapHelper.BuildSearchDocument(fv),
                CardTemplateId = sourceCard.CardTemplateId,
                ProjectTermId = sourceCard.ProjectTermId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Cards.Add(forkedCard);
        }

        forkedDeck.CardCount = sourceCards.Count;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deck {SourceDeckId} forked to {ForkedDeckId} by {UserId}",
            sourceDeck.Id, forkedDeck.Id, userId);

        return forkedDeck.Id;
    }

    /// <summary>
    /// Получает список опубликованных колод (SR-PUB-01)
    /// </summary>
    public async Task<(List<PublishedDeckDto> Decks, int TotalCount)> GetPublishedDecksAsync(
        Guid userId,
        Guid? authorId,
        string? searchQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Decks
            .Include(d => d.Project)
            .Where(d => d.IsPublic);

        if (authorId.HasValue)
        {
            query = query.Where(d => d.OwnerId == authorId.Value);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(d => d.Title.Contains(searchQuery) || 
                (d.Description != null && d.Description.Contains(searchQuery)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var decks = await query
            .OrderByDescending(d => d.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var deckDtos = decks.Select(d => new PublishedDeckDto
        {
            Id = d.Id,
            ProjectId = d.ProjectId,
            Title = d.Title,
            Description = d.Description,
            CoverImageUrl = d.CoverImageUrl,
            Author = new AuthorInfoDto
            {
                UserId = d.OwnerId,
                DisplayName = null // Would come from user service
            },
            CardCount = d.CardCount,
            LicenseType = d.LicenseType,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();

        return (deckDtos, totalCount);
    }

    /// <summary>
    /// Получает профиль автора (SR-PUB-04)
    /// </summary>
    public async Task<AuthorProfileDto> GetAuthorProfileAsync(
        Guid userId,
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        // Count published decks
        var publishedDecksCount = await _context.Decks
            .Where(d => d.OwnerId == authorId && d.IsPublic)
            .CountAsync(cancellationToken);

        // Get products and calculate sales
        var products = await _context.Products
            .Where(p => p.AuthorId == authorId && p.Status == "PUBLISHED")
            .ToListAsync(cancellationToken);

        var totalSales = products.Sum(p => p.SalesCount);

        // Calculate average rating
        var reviews = await _context.ProductReviews
            .Where(r => products.Select(p => p.Id).Contains(r.ProductId))
            .ToListAsync(cancellationToken);

        var averageRating = reviews.Count > 0
            ? reviews.Average(r => r.Rating)
            : 0.0;

        return new AuthorProfileDto
        {
            AuthorId = authorId,
            DisplayName = null, // Would come from user service
            PublishedDecksCount = publishedDecksCount,
            TotalSales = totalSales,
            AverageRating = averageRating
        };
    }

    // ============================================================================
    // Marketplace (SR-MKT-01 до SR-MKT-06)
    // ============================================================================

    /// <summary>
    /// Создает товар (SR-MKT-01)
    /// </summary>
    public async Task<Guid> CreateProductAsync(
        Guid userId,
        CreateProductDto request,
        CancellationToken cancellationToken = default)
    {
        // Verify deck belongs to user
        var deck = await _context.Decks
            .FirstOrDefaultAsync(d => d.Id == request.DeckId && d.OwnerId == userId, cancellationToken);

        if (deck == null)
        {
            throw new KeyNotFoundException($"Deck {request.DeckId} not found or access denied");
        }

        // Check if product already exists for this deck
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.LinkedDeckId == request.DeckId, cancellationToken);

        if (existingProduct != null)
        {
            throw new InvalidOperationException($"Product already exists for deck {request.DeckId}");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            AuthorId = userId,
            LinkedDeckId = request.DeckId,
            Title = request.Title,
            DescriptionHtml = request.DescriptionHtml,
            CoverImageUrl = request.CoverImageUrl,
            Price = request.Price,
            Currency = request.Currency,
            Status = "DRAFT",
            AverageRating = 0,
            ReviewCount = 0,
            SalesCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product created: {ProductId}, Deck: {DeckId}, Price: {Price} {Currency}",
            product.Id, request.DeckId, request.Price, request.Currency);

        return product.Id;
    }

    /// <summary>
    /// Обновляет товар (SR-MKT-01)
    /// </summary>
    public async Task<bool> UpdateProductAsync(
        Guid userId,
        Guid productId,
        UpdateProductDto request,
        CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.AuthorId == userId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product {productId} not found or access denied");
        }

        if (request.Title != null)
            product.Title = request.Title;
        if (request.DescriptionHtml != null)
            product.DescriptionHtml = request.DescriptionHtml;
        if (request.CoverImageUrl != null)
            product.CoverImageUrl = request.CoverImageUrl;
        if (request.Price.HasValue)
            product.Price = request.Price.Value;
        if (request.Currency != null)
            product.Currency = request.Currency;
        if (request.Status != null)
            product.Status = request.Status;

        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product {ProductId} updated", productId);

        return true;
    }

    /// <summary>
    /// Получает список товаров (SR-MKT-01)
    /// </summary>
    public async Task<(List<ProductDto> Products, int TotalCount)> GetProductsAsync(
        Guid userId,
        Guid? authorId,
        string? searchQuery,
        decimal? minPrice,
        decimal? maxPrice,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.LinkedDeck)
            .Where(p => p.Status == "PUBLISHED");

        if (authorId.HasValue)
        {
            query = query.Where(p => p.AuthorId == authorId.Value);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(p => p.Title.Contains(searchQuery) ||
                (p.DescriptionHtml != null && p.DescriptionHtml.Contains(searchQuery)));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            DeckId = p.LinkedDeckId,
            Author = new AuthorInfoDto
            {
                UserId = p.AuthorId,
                DisplayName = null // Would come from user service
            },
            Title = p.Title,
            DescriptionHtml = p.DescriptionHtml,
            CoverImageUrl = p.CoverImageUrl,
            Price = p.Price,
            Currency = p.Currency,
            Status = p.Status,
            AverageRating = p.AverageRating,
            ReviewCount = p.ReviewCount,
            SalesCount = p.SalesCount,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        return (productDtos, totalCount);
    }

    /// <summary>
    /// Получает детали товара с preview (SR-MKT-02)
    /// </summary>
    public async Task<(ProductDto Product, List<Card> Preview)> GetProductDetailsAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.LinkedDeck)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product {productId} not found");
        }

        var productDto = new ProductDto
        {
            Id = product.Id,
            DeckId = product.LinkedDeckId,
            Author = new AuthorInfoDto
            {
                UserId = product.AuthorId,
                DisplayName = null
            },
            Title = product.Title,
            DescriptionHtml = product.DescriptionHtml,
            CoverImageUrl = product.CoverImageUrl,
            Price = product.Price,
            Currency = product.Currency,
            Status = product.Status,
            AverageRating = product.AverageRating,
            ReviewCount = product.ReviewCount,
            SalesCount = product.SalesCount,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        // Get first N cards for preview (SR-MKT-02)
        const int previewCount = 10;
        var previewCards = await _context.Cards
            .Include(c => c.Deck)
            .Include(c => c.Note)
            .Where(c => c.DeckId == product.LinkedDeckId)
            .OrderBy(c => c.CreatedAt)
            .Take(previewCount)
            .ToListAsync(cancellationToken);

        return (productDto, previewCards);
    }

    /// <summary>
    /// Создает отзыв (SR-MKT-05)
    /// </summary>
    public async Task<Guid> CreateReviewAsync(
        Guid userId,
        CreateReviewDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5");
        }

        var product = await _context.Products
            .Include(p => p.LinkedDeck)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product {request.ProductId} not found");
        }

        // Check entitlement (Verified Purchase) (SR-MKT-05)
        var entitlement = await _entitlementService.CheckEntitlementAsync(
            userId,
            product.LinkedDeckId,
            CancellationToken.None);

        if (!entitlement.HasAccess || entitlement.Source != "PURCHASE")
        {
            throw new UnauthorizedAccessException("Only verified purchasers can leave reviews");
        }

        // Check if review already exists
        var existingReview = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.ProductId == request.ProductId && r.UserId == userId, cancellationToken);

        if (existingReview != null)
        {
            throw new InvalidOperationException("Review already exists for this product");
        }

        var review = new ProductReview
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            UserId = userId,
            Rating = (short)request.Rating,
            Comment = request.Comment,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductReviews.Add(review);

        // Recalculate average rating and review count
        var allReviews = await _context.ProductReviews
            .Where(r => r.ProductId == request.ProductId)
            .ToListAsync(cancellationToken);

        product.AverageRating = allReviews.Count > 0 ? (float)allReviews.Average(r => r.Rating) : 0f;
        product.ReviewCount = allReviews.Count;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Review created: {ReviewId}, Product: {ProductId}, Rating: {Rating}",
            review.Id, request.ProductId, request.Rating);

        return review.Id;
    }

    /// <summary>
    /// Получает статистику товара (SR-MKT-06)
    /// </summary>
    public async Task<ProductStatsDto> GetProductStatsAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product {productId} not found");
        }

        // Check if user is the author
        if (product.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only product author can view stats");
        }

        return new ProductStatsDto
        {
            ProductId = product.Id,
            SalesCount = product.SalesCount,
            ReviewsCount = product.ReviewCount,
            AverageRating = product.AverageRating,
            RetentionRate = null // Would require additional data
        };
    }

    /// <summary>
    /// Проверяет право доступа (SR-MKT-03, SR-COL-07)
    /// </summary>
    public async Task<EntitlementDto> CheckEntitlementAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken = default)
    {
        return await _entitlementService.CheckEntitlementAsync(userId, deckId, cancellationToken);
    }

    // ============================================================================
    // Automatic Detachment (SR-COL-08)
    // ============================================================================

    /// <summary>
    /// Создает Fork для всех активных пользователей при удалении/скрытии публичной колоды (SR-COL-08)
    /// </summary>
    public async Task<int> DetachActiveUsersAsync(
        Guid deckId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var deck = await _context.Decks
            .FirstOrDefaultAsync(d => d.Id == deckId, cancellationToken);

        if (deck == null)
        {
            throw new KeyNotFoundException($"Deck {deckId} not found");
        }

        // Найти активных пользователей
        var activeUsers = await FindActiveUsersAsync(deckId, ownerId, cancellationToken);

        if (activeUsers.Count == 0)
        {
            _logger.LogInformation(
                "No active users found for deck {DeckId}, skipping detachment",
                deckId);
            return 0;
        }

        _logger.LogInformation(
            "Detaching {Count} active users from deck {DeckId}",
            activeUsers.Count, deckId);

        int successCount = 0;
        int failureCount = 0;

        // Обрабатываем каждого пользователя отдельно, чтобы ошибка одного не блокировала других
        foreach (var userId in activeUsers)
        {
            try
            {
                await DetachUserAsync(userId, deckId, ownerId, deck, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to detach user {UserId} from deck {DeckId}",
                    userId, deckId);
                failureCount++;
            }
        }

        _logger.LogInformation(
            "Detachment completed for deck {DeckId}: {SuccessCount} successful, {FailureCount} failed",
            deckId, successCount, failureCount);

        return successCount;
    }

    /// <summary>
    /// Находит всех активных пользователей для колоды
    /// </summary>
    private async Task<List<Guid>> FindActiveUsersAsync(
        Guid deckId,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        // Пользователи с прогрессом
        var usersWithProgress = await _context.UserCardProgresses
            .Where(ucp => ucp.Card.DeckId == deckId && ucp.UserId != ownerId)
            .Select(ucp => ucp.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Пользователи с принятыми вкладами
        var contributors = await _context.Contributions
            .Where(c => c.TargetDeckId == deckId && c.Status == "MERGED" && c.AuthorId != ownerId)
            .Select(c => c.AuthorId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Покупатели
        var buyers = await _context.UserEntitlements
            .Where(e => e.DeckId == deckId && e.Source == "PURCHASE" && e.IsActive && e.UserId != ownerId)
            .Select(e => e.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Объединить всех активных пользователей
        var activeUsers = usersWithProgress
            .Union(contributors)
            .Union(buyers)
            .Distinct()
            .ToList();

        return activeUsers;
    }

    /// <summary>
    /// Создает Fork для одного пользователя с сохранением прогресса
    /// </summary>
    private async Task DetachUserAsync(
        Guid userId,
        Guid deckId,
        Guid ownerId,
        Deck sourceDeck,
        CancellationToken cancellationToken)
    {
        // Найти или создать проект пользователя
        var project = await FindOrCreateDefaultProjectAsync(userId, sourceDeck.Project, cancellationToken);

        // Создать Fork
        var forkRequest = new ForkDeckDto
        {
            DeckId = deckId,
            TargetProjectId = project.Id,
            NewTitle = $"{sourceDeck.Title} (Copy)"
        };

        var forkedDeckId = await ForkDeckAsync(userId, forkRequest, cancellationToken);

        // Получить маппинг card_id
        var cardMapping = await GetCardMappingAsync(deckId, forkedDeckId, cancellationToken);

        // Скопировать прогресс с remapping
        await CopyUserProgressAsync(userId, deckId, forkedDeckId, project.Id, cardMapping, cancellationToken);

        // Удалить подписки и entitlements
        await CleanupUserAccessAsync(userId, deckId, cancellationToken);

        _logger.LogInformation(
            "Successfully detached user {UserId} from deck {DeckId}, created fork {ForkedDeckId}",
            userId, deckId, forkedDeckId);
    }

    /// <summary>
    /// Находит или создает проект пользователя для Fork
    /// </summary>
    private async Task<Project> FindOrCreateDefaultProjectAsync(
        Guid userId,
        Project sourceProject,
        CancellationToken cancellationToken)
    {
        // Попытаться найти проект с тем же языком
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.UserId == userId && 
                p.SourceLang == sourceProject.SourceLang && 
                p.TargetLang == sourceProject.TargetLang, 
                cancellationToken);

        if (project != null)
        {
            return project;
        }

        // Если не найден, создать новый проект
        project = new Project
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = $"{sourceProject.Title} Project",
            SourceLang = sourceProject.SourceLang,
            TargetLang = sourceProject.TargetLang,
            FsrsSettings = sourceProject.FsrsSettings,
            Stats = new ProjectStats
            {
                TotalLemmas = 0,
                MatureLemmas = 0
            },
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created default project {ProjectId} for user {UserId}",
            project.Id, userId);

        return project;
    }

    /// <summary>
    /// Создает маппинг старых card_id -> новых card_id
    /// </summary>
    private static Dictionary<string, NoteFieldValue> CloneFieldMap(IReadOnlyDictionary<string, NoteFieldValue> src)
    {
        var d = new Dictionary<string, NoteFieldValue>(StringComparer.Ordinal);
        foreach (var kv in src)
        {
            d[kv.Key] = new NoteFieldValue
            {
                String = kv.Value.String,
                Strings = kv.Value.Strings != null ? [.. kv.Value.Strings] : null,
            };
        }

        return d;
    }

    private async Task<Dictionary<Guid, Guid>> GetCardMappingAsync(
        Guid sourceDeckId,
        Guid forkedDeckId,
        CancellationToken cancellationToken)
    {
        var sourceCards = await _context.Cards
            .Where(c => c.DeckId == sourceDeckId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var forkedCards = await _context.Cards
            .Where(c => c.DeckId == forkedDeckId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var mapping = new Dictionary<Guid, Guid>();

        var n = Math.Min(sourceCards.Count, forkedCards.Count);
        for (var i = 0; i < n; i++)
            mapping[sourceCards[i].Id] = forkedCards[i].Id;

        if (sourceCards.Count != forkedCards.Count)
        {
            _logger.LogWarning(
                "Card mapping: source count {Src} vs forked count {Dst} for deck mapping",
                sourceCards.Count, forkedCards.Count);
        }

        foreach (var sourceCard in sourceCards)
        {
            if (!mapping.ContainsKey(sourceCard.Id))
                _logger.LogWarning(
                    "Could not find matching forked card for source card {CardId} (search_document: {Doc})",
                    sourceCard.Id, sourceCard.SearchDocument);
        }

        _logger.LogInformation(
            "Created card mapping: {MappingCount} cards mapped from deck {SourceDeckId} to {ForkedDeckId}",
            mapping.Count, sourceDeckId, forkedDeckId);

        return mapping;
    }

    /// <summary>
    /// Копирует прогресс пользователя с remapping на новые card_id
    /// </summary>
    private async Task CopyUserProgressAsync(
        Guid userId,
        Guid sourceDeckId,
        Guid forkedDeckId,
        Guid projectId,
        Dictionary<Guid, Guid> cardMapping,
        CancellationToken cancellationToken)
    {
        var sourceProgress = await _context.UserCardProgresses
            .Where(ucp => ucp.UserId == userId && ucp.Card.DeckId == sourceDeckId)
            .Include(ucp => ucp.Card)
            .ToListAsync(cancellationToken);

        if (sourceProgress.Count == 0)
        {
            return;
        }

        int copiedCount = 0;

        foreach (var progress in sourceProgress)
        {
            if (cardMapping.TryGetValue(progress.CardId, out var newCardId))
            {
                // Проверить, не существует ли уже прогресс для новой карточки
                var existingProgress = await _context.UserCardProgresses
                    .FirstOrDefaultAsync(ucp => ucp.UserId == userId && ucp.CardId == newCardId, cancellationToken);

                if (existingProgress != null)
                {
                    // Обновить существующий прогресс, сохранив более поздние данные
                    if (progress.LastReview > existingProgress.LastReview)
                    {
                        existingProgress.State = progress.State;
                        existingProgress.Stability = progress.Stability;
                        existingProgress.Difficulty = progress.Difficulty;
                        existingProgress.Due = progress.Due;
                        existingProgress.ElapsedDays = progress.ElapsedDays;
                        existingProgress.ScheduledDays = progress.ScheduledDays;
                        existingProgress.Reps = progress.Reps;
                        existingProgress.Lapses = progress.Lapses;
                        existingProgress.IsSuspended = progress.IsSuspended;
                        existingProgress.LastReview = progress.LastReview;
                    }
                }
                else
                {
                    // Создать новый прогресс
                    var newProgress = new UserCardProgress
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        CardId = newCardId,
                        ProjectId = projectId,
                        State = progress.State,
                        Step = progress.Step,
                        Stability = progress.Stability,
                        Difficulty = progress.Difficulty,
                        Due = progress.Due,
                        ElapsedDays = progress.ElapsedDays,
                        ScheduledDays = progress.ScheduledDays,
                        Reps = progress.Reps,
                        Lapses = progress.Lapses,
                        IsSuspended = progress.IsSuspended,
                        LastReview = progress.LastReview
                    };

                    _context.UserCardProgresses.Add(newProgress);
                }

                copiedCount++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Copied {CopiedCount} progress records for user {UserId} from deck {SourceDeckId} to {ForkedDeckId}",
            copiedCount, userId, sourceDeckId, forkedDeckId);
    }

    /// <summary>
    /// Удаляет подписки и деактивирует entitlements для пользователя
    /// </summary>
    private async Task CleanupUserAccessAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken)
    {
        // Удалить подписки
        var subscriptions = await _context.DeckSubscriptions
            .Where(s => s.UserId == userId && s.DeckId == deckId)
            .ToListAsync(cancellationToken);

        if (subscriptions.Any())
        {
            _context.DeckSubscriptions.RemoveRange(subscriptions);
        }

        // Деактивировать entitlements (не удаляем, для истории)
        var entitlements = await _context.UserEntitlements
            .Where(e => e.UserId == userId && e.DeckId == deckId && e.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var entitlement in entitlements)
        {
            entitlement.IsActive = false;
        }

        if (subscriptions.Any() || entitlements.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cleaned up access for user {UserId} to deck {DeckId}: removed {SubCount} subscriptions, deactivated {EntCount} entitlements",
                userId, deckId, subscriptions.Count, entitlements.Count);
        }
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private ContributionDto MapToContributionDto(Contribution contribution)
    {
        return new ContributionDto
        {
            Id = contribution.Id,
            TargetDeckId = contribution.TargetDeckId,
            TargetCardId = contribution.TargetCardId,
            Author = new AuthorInfoDto
            {
                UserId = contribution.AuthorId,
                DisplayName = null // Would come from user service
            },
            Type = contribution.Type,
            Status = contribution.Status,
            Content = contribution.Payload,
            Comment = contribution.Comment,
            CreatedAt = contribution.CreatedAt,
            UpdatedAt = contribution.UpdatedAt
        };
    }
}
