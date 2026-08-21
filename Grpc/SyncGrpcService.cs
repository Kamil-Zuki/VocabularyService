using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Pvs.Content.Grpc;
using VocabularyService.Dtos.Sync;
using VocabularyService.Helpers;
using VocabularyService.Services;
using static Pvs.Content.Grpc.SyncService;

namespace VocabularyService.Grpc;

/// <summary>
/// gRPC сервис для синхронизации данных
/// </summary>
public class SyncGrpcService : SyncServiceBase
{
    private readonly ILogger<SyncGrpcService> _logger;
    private readonly ISyncService _syncService;
    private readonly IMapper _mapper;
    private readonly IValidator<SyncDataRequest> _syncDataValidator;
    private readonly IValidator<BatchSubmitReviewsRequest> _batchSubmitReviewsValidator;

    public SyncGrpcService(
        ILogger<SyncGrpcService> logger,
        ISyncService syncService,
        IMapper mapper,
        IValidator<SyncDataRequest> syncDataValidator,
        IValidator<BatchSubmitReviewsRequest> batchSubmitReviewsValidator)
    {
        _logger = logger;
        _syncService = syncService;
        _mapper = mapper;
        _syncDataValidator = syncDataValidator;
        _batchSubmitReviewsValidator = batchSubmitReviewsValidator;
    }

    //===== SR-SNC-01: Получение дельты изменений =====
    public override async Task<SyncDataResponse> SyncData(
        SyncDataRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        var validationResult = await _syncDataValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        try
        {
            var requestDto = new SyncDataRequestDto
            {
                LastSyncToken = request.LastSyncToken?.ToDateTime(),
                ProjectId = !string.IsNullOrEmpty(request.ProjectId) 
                    ? Guid.Parse(request.ProjectId) 
                    : null
            };

            var responseDto = await _syncService.SyncDataAsync(
                userId,
                requestDto,
                context.CancellationToken);

            return _mapper.Map<SyncDataResponse>(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing data for user {UserId}", userId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== SR-SNC-03: Пакетная отправка офлайн-ответов =====
    public override async Task<BatchSubmitReviewsResponse> BatchSubmitReviews(
        BatchSubmitReviewsRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        var validationResult = await _batchSubmitReviewsValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        try
        {
            var requestDto = _mapper.Map<BatchSubmitReviewsRequestDto>(request);

            var responseDto = await _syncService.BatchSubmitReviewsAsync(
                userId,
                requestDto,
                context.CancellationToken);

            return _mapper.Map<BatchSubmitReviewsResponse>(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch reviews for user {UserId}", userId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
