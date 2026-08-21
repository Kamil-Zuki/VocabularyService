using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class UpdateCardRequestValidator : AbstractValidator<UpdateCardRequest>
{
    public UpdateCardRequestValidator()
    {
        RuleFor(x => x.CardId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("Card ID must be a valid UUID");
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}
