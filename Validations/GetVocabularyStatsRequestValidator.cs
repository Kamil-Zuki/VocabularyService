using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class GetVocabularyStatsRequestValidator : AbstractValidator<GetVocabularyStatsRequest>
{
    public GetVocabularyStatsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("User ID must be a valid UUID");

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("Project ID must be a valid UUID");
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
