using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

/// <summary>
/// Валидатор для GetProjectsRequest
/// </summary>
public class GetProjectsRequestValidator : AbstractValidator<GetProjectsRequest>
{
    public GetProjectsRequestValidator()
    {
        // user_id опционален в запросе, так как берется из ServerCallContext
        // include_archived имеет дефолтное значение false, валидация не требуется
    }
}

