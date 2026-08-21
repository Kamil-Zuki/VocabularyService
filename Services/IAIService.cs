using VocabularyService.Dtos.AI;

namespace VocabularyService.Services;

public interface IAIService
{
    Task<GenerateContextResponseDto> GenerateContextAsync(
        Guid userId,
        GenerateContextRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ExplainGrammarResponseDto> ExplainGrammarAsync(
        Guid userId,
        ExplainGrammarRequestDto request,
        CancellationToken cancellationToken = default);
}
