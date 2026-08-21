using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Pvs.Content.Grpc;
using VocabularyService.Dtos.Community;
using VocabularyService.Services;
using GrpcCommunityService = Pvs.Content.Grpc.CommunityService;
using GrpcContributionDto = Pvs.Content.Grpc.ContributionDto;
using GrpcPublishedDeckDto = Pvs.Content.Grpc.PublishedDeckDto;
using GrpcProductDto = Pvs.Content.Grpc.ProductDto;

namespace VocabularyService.Grpc;

/// <summary>
/// gRPC сервис для коллаборации и маркетплейса
/// </summary>
public class CommunityGrpcService : GrpcCommunityService.CommunityServiceBase
{
    private readonly ICommunityService _communityService;
    private readonly IMapper _mapper;
    private readonly ILogger<CommunityGrpcService> _logger;
    private readonly IValidator<CreateContributionRequest> _createContributionValidator;
    private readonly IValidator<ResolveContributionRequest> _resolveContributionValidator;
    private readonly IValidator<PublishDeckRequest> _publishDeckValidator;
    private readonly IValidator<ForkDeckRequest> _forkDeckValidator;
    private readonly IValidator<CreateProductRequest> _createProductValidator;
    private readonly IValidator<CreateReviewRequest> _createReviewValidator;

    public CommunityGrpcService(
        ICommunityService communityService,
        IMapper mapper,
        ILogger<CommunityGrpcService> logger,
        IValidator<CreateContributionRequest> createContributionValidator,
        IValidator<ResolveContributionRequest> resolveContributionValidator,
        IValidator<PublishDeckRequest> publishDeckValidator,
        IValidator<ForkDeckRequest> forkDeckValidator,
        IValidator<CreateProductRequest> createProductValidator,
        IValidator<CreateReviewRequest> createReviewValidator)
    {
        _communityService = communityService;
        _mapper = mapper;
        _logger = logger;
        _createContributionValidator = createContributionValidator;
        _resolveContributionValidator = resolveContributionValidator;
        _publishDeckValidator = publishDeckValidator;
        _forkDeckValidator = forkDeckValidator;
        _createProductValidator = createProductValidator;
        _createReviewValidator = createReviewValidator;
    }

    // ============================================================================
    // Contributions
    // ============================================================================

    public override async Task<CreateContributionResponse> CreateContribution(
        CreateContributionRequest request,
        ServerCallContext context)
    {
        var validationResult = await _createContributionValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var userId = Guid.Parse(request.UserId);
        var dto = _mapper.Map<CreateContributionDto>(request);

        try
        {
            var contribution = await _communityService.CreateContributionAsync(userId, dto, context.CancellationToken);
            return new CreateContributionResponse
            {
                ContributionId = contribution.Id.ToString()
            };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<GetContributionsResponse> GetContributions(
        GetContributionsRequest request,
        ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var deckId = string.IsNullOrEmpty(request.DeckId) ? (Guid?)null : Guid.Parse(request.DeckId);
        var status = string.IsNullOrEmpty(request.Status) ? null : request.Status;

        var contributions = await _communityService.GetContributionsAsync(
            userId, deckId, status, request.Role, context.CancellationToken);

        var response = new GetContributionsResponse();
        response.Contributions.AddRange(contributions.Select(_mapper.Map<GrpcContributionDto>));

        return response;
    }

    public override async Task<GetContributionResponse> GetContribution(
        GetContributionRequest request,
        ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var contributionId = Guid.Parse(request.ContributionId);

        try
        {
            var (contribution, diff) = await _communityService.GetContributionAsync(
                userId, contributionId, context.CancellationToken);

            var response = new GetContributionResponse
            {
                Contribution = _mapper.Map<GrpcContributionDto>(contribution),
                Diff = _mapper.Map<ContributionDiff>(diff)
            };

            return response;
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
    }

    public override async Task<ResolveContributionResponse> ResolveContribution(
        ResolveContributionRequest request,
        ServerCallContext context)
    {
        var validationResult = await _resolveContributionValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var userId = Guid.Parse(request.UserId);
        var contributionId = Guid.Parse(request.ContributionId);
        var dto = _mapper.Map<ResolveContributionDto>(request);

        try
        {
            var (success, mergedCardId) = await _communityService.ResolveContributionAsync(
                userId, contributionId, dto, context.CancellationToken);

            var response = new ResolveContributionResponse { Success = success };
            if (mergedCardId.HasValue)
            {
                response.MergedCardId = mergedCardId.Value.ToString();
            }

            return response;
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<UpdateContributionPolicyResponse> UpdateContributionPolicy(
        UpdateContributionPolicyRequest request,
        ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var deckId = Guid.Parse(request.DeckId);

        try
        {
            var success = await _communityService.UpdateContributionPolicyAsync(
                userId, deckId, request.Policy, context.CancellationToken);

            return new UpdateContributionPolicyResponse { Success = success };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    // ============================================================================
    // Publishing
    // ============================================================================

    public override async Task<PublishDeckResponse> PublishDeck(
        PublishDeckRequest request,
        ServerCallContext context)
    {
        var validationResult = await _publishDeckValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var userId = Guid.Parse(request.UserId);
        var dto = _mapper.Map<PublishDeckDto>(request);

        try
        {
            var success = await _communityService.PublishDeckAsync(userId, dto, context.CancellationToken);

            return new PublishDeckResponse
            {
                DeckId = request.DeckId,
                IsPublic = success
            };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<ForkDeckResponse> ForkDeck(
        ForkDeckRequest request,
        ServerCallContext context)
    {
        var validationResult = await _forkDeckValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var userId = Guid.Parse(request.UserId);
        var dto = _mapper.Map<ForkDeckDto>(request);

        try
        {
            var forkedDeckId = await _communityService.ForkDeckAsync(userId, dto, context.CancellationToken);

            // Get deck to determine license type
            var deck = await _communityService.GetPublishedDecksAsync(
                userId, null, null, 1, 1, context.CancellationToken);

            return new ForkDeckResponse
            {
                DeckId = forkedDeckId.ToString(),
                ForkedFromId = request.DeckId,
                LicenseType = "PRIVATE" // Will be set correctly in service
            };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
    }

    public override async Task<GetPublishedDecksResponse> GetPublishedDecks(
        GetPublishedDecksRequest request,
        ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var authorId = string.IsNullOrEmpty(request.AuthorId) ? (Guid?)null : Guid.Parse(request.AuthorId);
        var searchQuery = string.IsNullOrEmpty(request.SearchQuery) ? null : request.SearchQuery;

        var (decks, totalCount) = await _communityService.GetPublishedDecksAsync(
            userId, authorId, searchQuery, request.Page, request.PageSize, context.CancellationToken);

        var response = new GetPublishedDecksResponse
        {
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        response.Decks.AddRange(decks.Select(_mapper.Map<GrpcPublishedDeckDto>));

        return response;
    }

    public override async Task<GetAuthorProfileResponse> GetAuthorProfile(
        GetAuthorProfileRequest request,
        ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var authorId = Guid.Parse(request.AuthorId);

        try
        {
            var profile = await _communityService.GetAuthorProfileAsync(
                userId, authorId, context.CancellationToken);

            return _mapper.Map<GetAuthorProfileResponse>(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting author profile");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    // ============================================================================
    // Marketplace
    // ============================================================================

    public override async Task<CreateProductResponse> CreateProduct(
        CreateProductRequest request,
        ServerCallContext context)
    {
        var validationResult = await _createProductValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var userId = Guid.Parse(request.UserId);
        var dto = _mapper.Map<CreateProductDto>(request);

        try
        {
            var productId = await _communityService.CreateProductAsync(userId, dto, context.CancellationToken);

            return new CreateProductResponse
            {
                ProductId = productId.ToString()
            };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<UpdateProductResponse> UpdateProduct(
        UpdateProductRequest request,
        ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var productId = Guid.Parse(request.ProductId);
        var dto = _mapper.Map<UpdateProductDto>(request);

        try
        {
            var success = await _communityService.UpdateProductAsync(userId, productId, dto, context.CancellationToken);

            return new UpdateProductResponse
            {
                ProductId = request.ProductId
            };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<GetProductsResponse> GetProducts(
        GetProductsRequest request,
        ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var authorId = string.IsNullOrEmpty(request.AuthorId) ? (Guid?)null : Guid.Parse(request.AuthorId);
        var searchQuery = string.IsNullOrEmpty(request.SearchQuery) ? null : request.SearchQuery;
        var minPrice = request.MinPrice.HasValue ? (decimal?)request.MinPrice.Value : null;
        var maxPrice = request.MaxPrice.HasValue ? (decimal?)request.MaxPrice.Value : null;

        var (products, totalCount) = await _communityService.GetProductsAsync(
            userId, authorId, searchQuery, minPrice, maxPrice, request.Page, request.PageSize, context.CancellationToken);

        var response = new GetProductsResponse
        {
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        response.Products.AddRange(products.Select(_mapper.Map<GrpcProductDto>));

        return response;
    }

    public override async Task<GetProductDetailsResponse> GetProductDetails(
        GetProductDetailsRequest request,
        ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var productId = Guid.Parse(request.ProductId);

        try
        {
            var (product, preview) = await _communityService.GetProductDetailsAsync(
                userId, productId, context.CancellationToken);

            var response = new GetProductDetailsResponse
            {
                Product = _mapper.Map<GrpcProductDto>(product)
            };

            response.DeckPreview.AddRange(preview.Select(_mapper.Map<CardPreview>));

            return response;
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<CreateReviewResponse> CreateReview(
        CreateReviewRequest request,
        ServerCallContext context)
    {
        var validationResult = await _createReviewValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))));
        }

        var userId = Guid.Parse(request.UserId);
        var dto = _mapper.Map<CreateReviewDto>(request);

        try
        {
            var reviewId = await _communityService.CreateReviewAsync(userId, dto, context.CancellationToken);

            return new CreateReviewResponse
            {
                ReviewId = reviewId.ToString()
            };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<GetProductStatsResponse> GetProductStats(
        GetProductStatsRequest request,
        ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var productId = Guid.Parse(request.ProductId);

        try
        {
            var stats = await _communityService.GetProductStatsAsync(
                userId, productId, context.CancellationToken);

            return _mapper.Map<GetProductStatsResponse>(stats);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
    }

    public override async Task<CheckEntitlementResponse> CheckEntitlement(
        CheckEntitlementRequest request,
        ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);
        var deckId = Guid.Parse(request.DeckId);

        var entitlement = await _communityService.CheckEntitlementAsync(
            userId, deckId, context.CancellationToken);

        return _mapper.Map<CheckEntitlementResponse>(entitlement);
    }
}
