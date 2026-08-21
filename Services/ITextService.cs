using VocabularyService.Dtos.Text;

namespace VocabularyService.Services;

public interface ITextService
{
    Task<AnalyzeTextResponseDto> AnalyzeTextAsync(
        Guid userId,
        AnalyzeTextRequestDto request,
        CancellationToken cancellationToken = default);
}
