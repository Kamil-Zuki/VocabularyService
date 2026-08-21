using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class PublishDeckRequestValidator : AbstractValidator<PublishDeckRequest>
{
    public PublishDeckRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.DeckId)
            .NotEmpty()
            .WithMessage("Deck ID is required");

        RuleFor(x => x.LicenseType)
            .NotEmpty()
            .Must(lt => lt == "FREE" || lt == "COMMERCIAL")
            .WithMessage("LicenseType must be FREE or COMMERCIAL");
    }
}
