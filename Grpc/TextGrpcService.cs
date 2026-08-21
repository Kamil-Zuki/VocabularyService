using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Pvs.Content.Grpc;
using VocabularyService.Dtos.Text;
using VocabularyService.Helpers;
using VocabularyService.Services;
using static Pvs.Content.Grpc.TextService;

namespace VocabularyService.Grpc;

public class TextGrpcService : TextServiceBase
{
    private readonly ILogger<TextGrpcService> _logger;
    private readonly ITextService _textService;
    private readonly IMapper _mapper;
    private readonly IValidator<AnalyzeTextRequest> _analyzeTextValidator;

    public TextGrpcService(
        ILogger<TextGrpcService> logger,
        ITextService textService,
        IMapper mapper,
        IValidator<AnalyzeTextRequest> analyzeTextValidator)
    {
        _logger = logger;
        _textService = textService;
        _mapper = mapper;
        _analyzeTextValidator = analyzeTextValidator;
    }

    public override async Task<AnalyzeTextResponse> AnalyzeText(
        AnalyzeTextRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        var validationResult = await _analyzeTextValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        try
        {
            var requestDto = _mapper.Map<AnalyzeTextRequestDto>(request);

            var responseDto = await _textService.AnalyzeTextAsync(
                userId,
                requestDto,
                context.CancellationToken);

            return _mapper.Map<AnalyzeTextResponse>(responseDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation for user {UserId}", userId);
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing text for user {UserId}", userId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
