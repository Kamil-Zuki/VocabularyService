using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Pvs.Content.Grpc;
using VocabularyService.Dtos.AI;
using VocabularyService.Helpers;
using VocabularyService.Services;
using static Pvs.Content.Grpc.AIService;

namespace VocabularyService.Grpc;

public class AIGrpcService : AIServiceBase
{
    private readonly ILogger<AIGrpcService> _logger;
    private readonly IAIService _aiService;
    private readonly IBillingLimitService _billingLimitService;
    private readonly IMapper _mapper;
    private readonly IValidator<GenerateContextRequest> _generateContextValidator;
    private readonly IValidator<ExplainGrammarRequest> _explainGrammarValidator;

    public AIGrpcService(
        ILogger<AIGrpcService> logger,
        IAIService aiService,
        IBillingLimitService billingLimitService,
        IMapper mapper,
        IValidator<GenerateContextRequest> generateContextValidator,
        IValidator<ExplainGrammarRequest> explainGrammarValidator)
    {
        _logger = logger;
        _aiService = aiService;
        _billingLimitService = billingLimitService;
        _mapper = mapper;
        _generateContextValidator = generateContextValidator;
        _explainGrammarValidator = explainGrammarValidator;
    }

    public override async Task<GenerateContextResponse> GenerateContext(
        GenerateContextRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        var validationResult = await _generateContextValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        if (!await _billingLimitService.CanUseAiAsync(userId, context.CancellationToken))
        {
            throw new RpcException(new Status(StatusCode.ResourceExhausted,
                "Daily AI request limit exceeded for your plan"));
        }

        try
        {
            var requestDto = _mapper.Map<GenerateContextRequestDto>(request);

            var responseDto = await _aiService.GenerateContextAsync(
                userId,
                requestDto,
                context.CancellationToken);

            await _billingLimitService.RecordAiRequestAsync(userId, context.CancellationToken);

            return _mapper.Map<GenerateContextResponse>(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating context for user {UserId}", userId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<ExplainGrammarResponse> ExplainGrammar(
        ExplainGrammarRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        var validationResult = await _explainGrammarValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        if (!await _billingLimitService.CanUseAiAsync(userId, context.CancellationToken))
        {
            throw new RpcException(new Status(StatusCode.ResourceExhausted,
                "Daily AI request limit exceeded for your plan"));
        }

        try
        {
            var requestDto = _mapper.Map<ExplainGrammarRequestDto>(request);

            var responseDto = await _aiService.ExplainGrammarAsync(
                userId,
                requestDto,
                context.CancellationToken);

            await _billingLimitService.RecordAiRequestAsync(userId, context.CancellationToken);

            return _mapper.Map<ExplainGrammarResponse>(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining grammar for user {UserId}", userId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
