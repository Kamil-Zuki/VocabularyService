using AutoMapper;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Pvs.Content.Grpc;
using VocabularyService.Dtos.Study;
using VocabularyService.Helpers;
using VocabularyService.Services;
using static Pvs.Content.Grpc.StudyService;

namespace VocabularyService.Grpc;

/// <summary>
/// gRPC сервис для работы с обучением
/// </summary>
public class StudyGrpcService : StudyServiceBase
{
    private readonly ILogger<StudyGrpcService> _logger;
    private readonly IStudyService _studyService;
    private readonly IMapper _mapper;
    private readonly IValidator<StartStudySessionRequest> _startSessionValidator;
    private readonly IValidator<SubmitReviewRequest> _submitReviewValidator;

    public StudyGrpcService(
        ILogger<StudyGrpcService> logger,
        IStudyService studyService,
        IMapper mapper,
        IValidator<StartStudySessionRequest> startSessionValidator,
        IValidator<SubmitReviewRequest> submitReviewValidator)
    {
        _logger = logger;
        _studyService = studyService;
        _mapper = mapper;
        _startSessionValidator = startSessionValidator;
        _submitReviewValidator = submitReviewValidator;
    }

    //===== SR-LRN-01: Старт новой сессии обучения =====
    public override async Task<StartStudySessionResponse> StartStudySession(
        StartStudySessionRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        var validationResult = await _startSessionValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        if (!Guid.TryParse(request.ProjectId, out var projectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Project ID format"));
        }

        Guid? deckId = null;
        if (!string.IsNullOrEmpty(request.DeckId))
        {
            if (!Guid.TryParse(request.DeckId, out var parsedDeckId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Deck ID format"));
            }
            deckId = parsedDeckId;
        }

        try
        {
            var sessionDto = await _studyService.StartStudySessionAsync(
                userId,
                projectId,
                deckId,
                context.CancellationToken);

            return _mapper.Map<StartStudySessionResponse>(sessionDto);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting study session");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== SR-LRN-02: Получение следующей карточки =====
    public override async Task<GetNextCardResponse> GetNextCard(
        GetNextCardRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Session ID format"));
        }

        try
        {
            var cardDto = await _studyService.GetNextCardAsync(
                sessionId,
                userId,
                context.CancellationToken);

            // Сессия закончилась — возвращаем пустой ответ (204 на уровне REST), без ошибки
            if (cardDto == null)
            {
                return new GetNextCardResponse();
            }

            var response = new GetNextCardResponse
            {
                Card = _mapper.Map<Pvs.Content.Grpc.CardStudyDto>(cardDto)
            };

            return response;
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next card");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== SR-LRN-03: Отправка оценки (FSRS) =====
    public override async Task<SubmitReviewResponse> SubmitReview(
        SubmitReviewRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        var validationResult = await _submitReviewValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Session ID format"));
        }

        if (!Guid.TryParse(request.CardId, out var cardId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Card ID format"));
        }

        try
        {
            var responseDto = await _studyService.SubmitReviewAsync(
                sessionId,
                userId,
                cardId,
                request.Rating,
                request.DurationMs,
                userAnswer: null,
                context.CancellationToken);

            return _mapper.Map<SubmitReviewResponse>(responseDto);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting review");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== SR-LRN-08: Отмена последнего действия =====
    public override async Task<UndoReviewResponse> UndoReview(
        UndoReviewRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Session ID format"));
        }

        try
        {
            var undoDto = await _studyService.UndoReviewAsync(
                sessionId,
                userId,
                context.CancellationToken);

            return _mapper.Map<UndoReviewResponse>(undoDto);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing review");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
