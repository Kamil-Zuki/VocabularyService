using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Pvs.Content.Grpc;
using VocabularyService.Dtos.Subscriptions;
using VocabularyService.Helpers;
using VocabularyService.Services;
using static Pvs.Content.Grpc.SubscriptionService;

namespace VocabularyService.Grpc;

/// <summary>
/// gRPC сервис для подписок на колоды.
/// </summary>
public class SubscriptionGrpcService : SubscriptionServiceBase
{
    private readonly ILogger<SubscriptionGrpcService> _logger;
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionGrpcService(
        ILogger<SubscriptionGrpcService> logger,
        ISubscriptionService subscriptionService)
    {
        _logger = logger;
        _subscriptionService = subscriptionService;
    }

    public override async Task<ListSubscriptionsResponse> ListSubscriptions(
        ListSubscriptionsRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        _logger.LogInformation("ListSubscriptions request from user {UserId}", userId);

        try
        {
            IReadOnlyList<SubscriptionListItemDto> items = await _subscriptionService.ListAsync(
                userId,
                context.CancellationToken);

            var response = new ListSubscriptionsResponse();

            foreach (var item in items)
            {
                response.Items.Add(MapToResponse(item));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing subscriptions for user {UserId}", userId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<SubscriptionItemResponse> Subscribe(
        SubscribeRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.DeckId, out var deckId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid deck ID format"));
        }

        _logger.LogInformation("Subscribe request from user {UserId} for deck {DeckId}", userId, deckId);

        try
        {
            var dto = await _subscriptionService.SubscribeAsync(userId, deckId, context.CancellationToken);
            return MapToResponse(dto);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to deck {DeckId} for user {UserId}", deckId, userId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<Empty> Unsubscribe(
        UnsubscribeRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.DeckId, out var deckId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid deck ID format"));
        }

        _logger.LogInformation("Unsubscribe request from user {UserId} for deck {DeckId}", userId, deckId);

        try
        {
            await _subscriptionService.UnsubscribeAsync(userId, deckId, context.CancellationToken);
            return new Empty();
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from deck {DeckId} for user {UserId}", deckId, userId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    private static SubscriptionItemResponse MapToResponse(SubscriptionListItemDto item)
    {
        return new SubscriptionItemResponse
        {
            DeckId = item.DeckId.ToString(),
            ProjectId = item.ProjectId.ToString(),
            Title = item.Title,
            SubscribedAt = Timestamp.FromDateTime(item.SubscribedAt.ToUniversalTime()),
            LastAccessedAt = Timestamp.FromDateTime((item.LastAccessedAt ?? item.SubscribedAt).ToUniversalTime()),
            LastSyncedVersion = item.LastSyncedVersion
        };
    }
}

