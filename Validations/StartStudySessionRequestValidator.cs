using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

/// <summary>
/// Валидатор для StartStudySessionRequest
/// </summary>
public class StartStudySessionRequestValidator : AbstractValidator<StartStudySessionRequest>
{
    public StartStudySessionRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("User ID must be a valid UUID");

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("Project ID must be a valid UUID");

        RuleFor(x => x.DeckId)
            .Must(BeValidGuidOrEmpty)
            .When(x => !string.IsNullOrEmpty(x.DeckId))
            .WithMessage("Deck ID must be a valid UUID");
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }

    private static bool BeValidGuidOrEmpty(string? value)
    {
        return string.IsNullOrEmpty(value) || Guid.TryParse(value, out _);
    }
}
