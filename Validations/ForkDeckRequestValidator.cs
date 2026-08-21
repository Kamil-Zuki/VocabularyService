using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class ForkDeckRequestValidator : AbstractValidator<ForkDeckRequest>
{
    public ForkDeckRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.DeckId)
            .NotEmpty()
            .WithMessage("Deck ID is required");

        RuleFor(x => x.TargetProjectId)
            .NotEmpty()
            .WithMessage("Target project ID is required");
    }
}
