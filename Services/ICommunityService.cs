using VocabularyService.Data.Entities;
using VocabularyService.Dtos.Community;

namespace VocabularyService.Services;

/// <summary>
/// Интерфейс сервиса для коллаборации и маркетплейса
/// </summary>
public interface ICommunityService
{
    // Contributions (SR-COL-01 до SR-COL-08)
    Task<ContributionDto> CreateContributionAsync(
        Guid userId,
        CreateContributionDto request,
        CancellationToken cancellationToken = default);

    Task<List<ContributionDto>> GetContributionsAsync(
        Guid userId,
        Guid? deckId,
        string? status,
        string role, // AUTHOR or MODERATOR
        CancellationToken cancellationToken = default);

    Task<(ContributionDto Contribution, ContributionDiffDto Diff)> GetContributionAsync(
        Guid userId,
        Guid contributionId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, Guid? MergedCardId)> ResolveContributionAsync(
        Guid userId,
        Guid contributionId,
        ResolveContributionDto request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateContributionPolicyAsync(
        Guid userId,
        Guid deckId,
        string policy, // OPEN, RESTRICTED, CLOSED
        CancellationToken cancellationToken = default);

    // Publishing (SR-PUB-01 до SR-PUB-04)
    Task<bool> PublishDeckAsync(
        Guid userId,
        PublishDeckDto request,
        CancellationToken cancellationToken = default);

    Task<Guid> ForkDeckAsync(
        Guid userId,
        ForkDeckDto request,
        CancellationToken cancellationToken = default);

    Task<(List<PublishedDeckDto> Decks, int TotalCount)> GetPublishedDecksAsync(
        Guid userId,
        Guid? authorId,
        string? searchQuery,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AuthorProfileDto> GetAuthorProfileAsync(
        Guid userId,
        Guid authorId,
        CancellationToken cancellationToken = default);

    // Marketplace (SR-MKT-01 до SR-MKT-06)
    Task<Guid> CreateProductAsync(
        Guid userId,
        CreateProductDto request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateProductAsync(
        Guid userId,
        Guid productId,
        UpdateProductDto request,
        CancellationToken cancellationToken = default);

    Task<(List<ProductDto> Products, int TotalCount)> GetProductsAsync(
        Guid userId,
        Guid? authorId,
        string? searchQuery,
        decimal? minPrice,
        decimal? maxPrice,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(ProductDto Product, List<Card> Preview)> GetProductDetailsAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateReviewAsync(
        Guid userId,
        CreateReviewDto request,
        CancellationToken cancellationToken = default);

    Task<ProductStatsDto> GetProductStatsAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<EntitlementDto> CheckEntitlementAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken = default);

    // Automatic Detachment (SR-COL-08)
    Task<int> DetachActiveUsersAsync(
        Guid deckId,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}
