using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

/// <summary>
/// Валидатор для SubmitReviewRequest
/// </summary>
public class SubmitReviewRequestValidator : AbstractValidator<SubmitReviewRequest>
{
    public SubmitReviewRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("User ID must be a valid UUID");

        RuleFor(x => x.SessionId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("Session ID must be a valid UUID");

        RuleFor(x => x.CardId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("Card ID must be a valid UUID");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 4)
            .WithMessage("Rating must be between 1 (Again) and 4 (Easy)");

        RuleFor(x => x.DurationMs)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Duration must be non-negative");
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
