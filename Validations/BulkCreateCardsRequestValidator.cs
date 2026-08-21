using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

/// <summary>
/// Валидатор для BulkCreateCardsRequest
/// </summary>
public class BulkCreateCardsRequestValidator : AbstractValidator<BulkCreateCardsRequest>
{
    public BulkCreateCardsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("User ID must be a valid UUID");

        RuleFor(x => x.DeckId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("Deck ID must be a valid UUID");

        RuleFor(x => x.Cards)
            .NotEmpty()
            .WithMessage("Cards list cannot be empty")
            .Must(x => x.Count <= 100)
            .WithMessage("Cannot create more than 100 cards at once");

        RuleForEach(x => x.Cards).SetValidator(new CreateCardRequestValidator());
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
