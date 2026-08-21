using AutoMapper;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Pvs.Content.Grpc;
using VocabularyService.Data.Entities;
using VocabularyService.Domain;
using VocabularyService.Dtos.Cards;
using VocabularyService.Helpers;
using VocabularyService.Services;
using static Pvs.Content.Grpc.CardService;

namespace VocabularyService.Grpc;

/// <summary>
/// gRPC сервис для работы с карточками
/// </summary>
public class CardGrpcService : CardServiceBase
{
    private readonly ILogger<CardGrpcService> _logger;
    private readonly ICardService _cardService;
    private readonly INoteTypeService _noteTypeService;
    private readonly IBillingLimitService _billingLimitService;
    private readonly IMapper _mapper;
    private readonly IValidator<BulkCreateCardsRequest> _bulkCreateCardsValidator;

    public CardGrpcService(
        ILogger<CardGrpcService> logger,
        ICardService cardService,
        INoteTypeService noteTypeService,
        IBillingLimitService billingLimitService,
        IMapper mapper,
        IValidator<BulkCreateCardsRequest> bulkCreateCardsValidator)
    {
        _logger = logger;
        _cardService = cardService;
        _noteTypeService = noteTypeService;
        _billingLimitService = billingLimitService;
        _mapper = mapper;
        _bulkCreateCardsValidator = bulkCreateCardsValidator;
    }

    //===== SR-VOC-01: Создание карточки вручную =====
    public override async Task<CardResponse> CreateCard(CreateCardRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        
        _logger.LogInformation("CreateCard request from user {UserId} for deck {DeckId}", userId, request.DeckId);

        if (!await _billingLimitService.CanCreateCardAsync(userId, context.CancellationToken))
        {
            throw new RpcException(new Status(StatusCode.ResourceExhausted,
                "Billing limit exceeded: maxCards"));
        }

        var dto = _mapper.Map<CreateCardDto>(request);
        dto.UserId = userId;

        try
        {
            var card = await _cardService.CreateCardAsync(dto, context.CancellationToken);
            var response = _mapper.Map<CardResponse>(card);
            response.SrsStatus = SrsStatus.New;
            return response;
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating card");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<CheckCardDuplicatesResponse> CheckCardDuplicates(CheckCardDuplicatesRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.ProjectId, out var projectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Project ID format"));
        }

        var dto = new CheckCardDuplicatesRequestDto
        {
            ProjectId = projectId,
            TermText = request.TermText,
        };

        try
        {
            var result = await _cardService.CheckDuplicatesAsync(userId, dto, context.CancellationToken);
            var response = new CheckCardDuplicatesResponse
            {
                IsDuplicate = result.IsDuplicate
            };

            if (!string.IsNullOrEmpty(result.NormalizedSurface))
            {
                response.NormalizedSurface = result.NormalizedSurface;
            }

            response.ExistingCards.AddRange(result.ExistingCards.Select(p => _mapper.Map<CardPreview>(p)));

            return response;
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duplicate cards");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== SR-API-01: Захват карточки из расширения =====
    public override async Task<CardResponse> CaptureCard(CaptureCardRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        
        _logger.LogInformation("CaptureCard request from user {UserId} for project {ProjectId}", userId, request.ProjectId);

        if (!await _billingLimitService.CanCreateCardAsync(userId, context.CancellationToken))
        {
            throw new RpcException(new Status(StatusCode.ResourceExhausted,
                "Billing limit exceeded: maxCards"));
        }

        var dto = _mapper.Map<CaptureCardDto>(request);
        dto.UserId = userId;

        try
        {
            var card = await _cardService.CaptureCardAsync(dto, context.CancellationToken);
            var response = _mapper.Map<CardResponse>(card);
            response.SrsStatus = SrsStatus.New;
            return response;
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing card");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    // Deprecated media endpoints kept for backward compatibility; actual storage is handled by media-service.
    public override Task<Pvs.Content.Grpc.UploadImageResponse> UploadImage(Pvs.Content.Grpc.UploadImageRequest request, ServerCallContext context)
    {
        throw new RpcException(new Status(StatusCode.Unimplemented, "Use media-service for image upload"));
    }

    public override Task<Pvs.Content.Grpc.UploadDocumentResponse> UploadDocument(Pvs.Content.Grpc.UploadDocumentRequest request, ServerCallContext context)
    {
        throw new RpcException(new Status(StatusCode.Unimplemented, "Use media-service for document upload"));
    }

    public override Task<Pvs.Content.Grpc.GetImageUrlResponse> GetImageUrl(Pvs.Content.Grpc.GetImageUrlRequest request, ServerCallContext context)
    {
        throw new RpcException(new Status(StatusCode.Unimplemented, "Use media-service for image URL resolution"));
    }

    public override async Task<GetNoteTypeForEditorResponse> GetNoteTypeForEditor(
        GetNoteTypeForEditorRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        if (!Guid.TryParse(request.ProjectId, out var projectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Project ID format"));
        }

        try
        {
            var nt = await _noteTypeService
                .GetSentenceMiningForEditorAsync(userId, projectId, context.CancellationToken)
                .ConfigureAwait(false);

            var response = new GetNoteTypeForEditorResponse();
            var payload = new NoteTypePayload
            {
                Id = nt.Id.ToString(),
                ProjectId = nt.ProjectId.ToString(),
                Name = nt.Name,
                Version = nt.Version,
            };
            foreach (var f in nt.NoteFields.OrderBy(x => x.SortOrder))
            {
                payload.Fields.Add(new NoteFieldDefinitionPayload
                {
                    FieldKey = f.FieldKey,
                    Label = f.Label,
                    FieldType = f.FieldType,
                    SortOrder = f.SortOrder,
                    Required = f.Required,
                    Archived = f.Archived,
                });
            }

            foreach (var t in nt.CardTemplates.OrderBy(x => x.SortOrder))
            {
                var tp = MapCardTemplate(t);
                payload.Templates.Add(tp);
            }

            response.NoteType = payload;
            var def = nt.CardTemplates.FirstOrDefault(x => x.TemplateKey == SentenceMiningNoteType.DefaultTemplateKey);
            if (def != null)
                response.DefaultTemplate = MapCardTemplate(def);

            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetNoteTypeForEditor failed");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    private static CardTemplatePayload MapCardTemplate(CardTemplate t) =>
        new()
        {
            Id = t.Id.ToString(),
            TemplateKey = t.TemplateKey,
            Name = t.Name,
            FrontTemplate = t.FrontTemplate,
            BackTemplate = t.BackTemplate,
            SortOrder = t.SortOrder,
            Enabled = t.Enabled,
        };

    //===== Получение карточки по ID =====
    public override async Task<CardResponse> GetCard(GetCardRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.CardId, out var cardId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Card ID format"));
        }

        try
        {
            var card = await _cardService.GetCardByIdAsync(cardId, userId, context.CancellationToken);
            if (card == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Card not found"));
            }

            var response = _mapper.Map<CardResponse>(card);
            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting card");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== SR-VOC-02: Обновление карточки =====
    public override async Task<CardResponse> UpdateCard(UpdateCardRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.CardId, out var cardId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Card ID format"));
        }

        var dto = _mapper.Map<UpdateCardDto>(request);

        try
        {
            var card = await _cardService.UpdateCardAsync(cardId, userId, dto, context.CancellationToken);
            var response = _mapper.Map<CardResponse>(card);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating card");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Удаление карточки =====
    public override async Task<Empty> DeleteCard(DeleteCardRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.CardId, out var cardId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Card ID format"));
        }

        try
        {
            await _cardService.DeleteCardAsync(cardId, userId, context.CancellationToken);
            return new Empty();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting card");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== SR-SRC-01: Полнотекстовый поиск =====
    public override async Task<SearchCardsResponse> SearchCards(SearchCardsRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        Guid? projectId = !string.IsNullOrEmpty(request.ProjectId) 
            ? Guid.Parse(request.ProjectId) 
            : null;
            
        Guid? deckId = !string.IsNullOrEmpty(request.DeckId) 
            ? Guid.Parse(request.DeckId) 
            : null;

        int pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var srsStatuses = request.SrsStatuses.Select(s => s.ToString().Replace("SRS_STATUS_", "")).ToList();

        try
        {
            var result = await _cardService.SearchCardsAsync(
                userId, 
                request.Query, 
                projectId, 
                deckId, 
                pageNumber, 
                pageSize, 
                srsStatuses,
                context.CancellationToken);

            var response = new SearchCardsResponse
            {
                PageNumber = pageNumber,
                TotalCount = result.TotalCount,
                TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize)
            };

            response.Items.AddRange(result.Items.Select(card => _mapper.Map<CardResponse>(card)));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cards");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Список карточек в колоде =====
    public override async Task<GetCardsByDeckResponse> GetCardsByDeck(GetCardsByDeckRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.DeckId, out var deckId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Deck ID format"));
        }

        int pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 20;

        try
        {
            var result = await _cardService.GetCardsByDeckAsync(
                userId, 
                deckId, 
                pageNumber, 
                pageSize, 
                context.CancellationToken);

            var response = new GetCardsByDeckResponse
            {
                PageNumber = pageNumber,
                TotalCount = result.TotalCount,
                TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize)
            };

            response.Items.AddRange(result.Items.Select(card => _mapper.Map<CardResponse>(card)));

            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cards by deck");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Массовое создание карточек (Импорт) =====
    public override async Task<BulkCreateCardsResponse> BulkCreateCards(BulkCreateCardsRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        
        var validationResult = await _bulkCreateCardsValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        if (!Guid.TryParse(request.DeckId, out var deckId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Deck ID format"));
        }

        var maxCards = await _billingLimitService.GetMaxCardsAsync(userId, context.CancellationToken);
        var currentCards = await _billingLimitService.GetCurrentCardCountAsync(userId, context.CancellationToken);
        
        if (currentCards + request.Cards.Count > maxCards)
        {
            throw new RpcException(new Status(StatusCode.ResourceExhausted,
                "Billing limit exceeded: maxCards"));
        }

        // Для массового создания user_id/deck_id берём из запроса и контекста, а не из каждой карты (часто пустые).
        var dtos = request.Cards.Select(c =>
        {
            var dto = _mapper.Map<CreateCardDto>(c);
            dto.UserId = userId;
            dto.DeckId = deckId;
            return dto;
        }).ToList();

        try
        {
            var cards = await _cardService.BulkCreateCardsAsync(userId, deckId, dtos, context.CancellationToken);
            var response = new BulkCreateCardsResponse();
            response.CreatedCards.AddRange(cards.Select(c => _mapper.Map<CardResponse>(c)));
            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk card creation");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Фоновый импорт =====
    public override async Task<StartImportJobResponse> StartImportJob(StartImportJobRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        var deckId = Guid.Parse(request.DeckId);
        
        // Normally we'd fetch project id, for simplicity just use empty or query it via a service
        var projectId = Guid.Empty; // TODO: fetch from deck

        var importService = context.GetHttpContext().RequestServices.GetRequiredService<IImportService>();
        var jobId = await importService.CreateJobAsync(userId, deckId, projectId, context.CancellationToken);
        
        // Start background execution
        _ = Task.Run(() => importService.ProcessImportJobAsync(jobId, request.DocumentId, request.FileName, request.ConfigJson, CancellationToken.None));

        return new StartImportJobResponse { JobId = jobId.ToString() };
    }

    public override async Task<GetImportJobStatusResponse> GetImportJobStatus(GetImportJobStatusRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        var jobId = Guid.Parse(request.JobId);

        var importService = context.GetHttpContext().RequestServices.GetRequiredService<IImportService>();
        var job = await importService.GetJobAsync(jobId, userId, context.CancellationToken);

        if (job == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Job not found"));

        return new GetImportJobStatusResponse
        {
            JobId = job.Id.ToString(),
            Status = job.Status,
            TotalRows = job.TotalRows,
            ProcessedRows = job.ProcessedRows,
            ErrorMessage = job.ErrorMessage ?? ""
        };
    }

    //===== Приостановка обучения карточки =====
    public override async Task<Empty> SuspendCard(SuspendCardRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.CardId, out var cardId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Card ID format"));
        }

        try
        {
            await _cardService.SuspendCardAsync(cardId, userId, context.CancellationToken);
            return new Empty();
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending card");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Возобновление обучения карточки =====
    public override async Task<Empty> UnsuspendCard(UnsuspendCardRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.CardId, out var cardId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Card ID format"));
        }

        try
        {
            await _cardService.UnsuspendCardAsync(cardId, userId, context.CancellationToken);
            return new Empty();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsuspending card");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Массовое удаление карточек =====
    public override async Task<Empty> BulkDeleteCards(BulkDeleteCardsRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        var cardIds = request.CardIds.Select(id => Guid.Parse(id)).ToList();

        try
        {
            await _cardService.BulkDeleteCardsAsync(userId, cardIds, context.CancellationToken);
            return new Empty();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting cards");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Перемещение карточек =====
    public override async Task<Empty> MoveCards(MoveCardsRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        var cardIds = request.CardIds.Select(id => Guid.Parse(id)).ToList();

        if (!Guid.TryParse(request.DeckId, out var deckId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Deck ID format"));
        }

        try
        {
            await _cardService.MoveCardsAsync(userId, cardIds, deckId, context.CancellationToken);
            return new Empty();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving cards");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Сброс прогресса карточек =====
    public override async Task<Empty> ResetCardProgress(ResetCardProgressRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        var cardIds = request.CardIds.Select(id => Guid.Parse(id)).ToList();

        try
        {
            await _cardService.ResetCardProgressAsync(userId, cardIds, context.CancellationToken);
            return new Empty();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting card progress");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Список сложных карточек =====
    public override async Task<GetLeechCardsResponse> GetLeechCards(GetLeechCardsRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.ProjectId, out var projectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Project ID format"));
        }

        int pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 20;
        int threshold = request.Threshold > 0 ? request.Threshold : 8;

        try
        {
            var result = await _cardService.GetLeechCardsAsync(
                userId, projectId, threshold, pageNumber, pageSize, context.CancellationToken);

            var response = new GetLeechCardsResponse
            {
                PageNumber = pageNumber,
                TotalCount = result.TotalCount,
                TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize)
            };
            response.Items.AddRange(result.Items.Select(c => _mapper.Map<CardResponse>(c)));
            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leech cards");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Список карточек без медиа =====
    public override async Task<GetCardsMissingMediaResponse> GetCardsMissingMedia(GetCardsMissingMediaRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.ProjectId, out var projectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Project ID format"));
        }

        int pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 20;

        try
        {
            var result = await _cardService.GetCardsMissingMediaAsync(
                userId, projectId, request.MediaType, pageNumber, pageSize, context.CancellationToken);

            var response = new GetCardsMissingMediaResponse
            {
                PageNumber = pageNumber,
                TotalCount = result.TotalCount,
                TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize)
            };
            response.Items.AddRange(result.Items.Select(c => _mapper.Map<CardResponse>(c)));
            return response;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cards missing media");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Получение превью карточек =====
    public override async Task<GetCardPreviewsResponse> GetCardPreviews(GetCardPreviewsRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        var cardIds = request.CardIds.Select(id => Guid.Parse(id)).ToList();

        try
        {
            var cards = await _cardService.GetCardPreviewsAsync(userId, cardIds, context.CancellationToken);
            var response = new GetCardPreviewsResponse();
            
            response.Previews.AddRange(cards.Select(card => _mapper.Map<CardPreview>(card)));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting card previews");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    private static SrsStatus MapSrsStatus(string status)
    {
        return status.ToUpperInvariant() switch
        {
            "NEW" => SrsStatus.New,
            "LEARNING" => SrsStatus.Learning,
            "REVIEW" => SrsStatus.Review,
            "RELEARNING" => SrsStatus.Relearning,
            "MATURE" => SrsStatus.Mature,
            _ => SrsStatus.New
        };
    }
}
