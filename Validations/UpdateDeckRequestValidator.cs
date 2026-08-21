using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

/// <summary>
/// Валидатор для UpdateDeckRequest
/// </summary>
public class UpdateDeckRequestValidator : AbstractValidator<UpdateDeckRequest>
{
    public UpdateDeckRequestValidator()
    {
        // Валидация DeckId
        RuleFor(x => x.DeckId)
            .NotEmpty()
            .WithMessage("Deck ID is required")
            .Must(BeValidGuid)
            .WithMessage("Deck ID must be a valid UUID");

        // Валидация Title (если указан)
        When(x => !string.IsNullOrEmpty(x.Title), () =>
        {
            RuleFor(x => x.Title)
                .Length(3, 100)
                .WithMessage("Title must be between 3 and 100 characters");
        });

        // Валидация ParentDeckId (если указан)
        When(x => !string.IsNullOrEmpty(x.ParentDeckId), () =>
        {
            RuleFor(x => x.ParentDeckId)
                .Must(BeValidGuid)
                .WithMessage("Parent deck ID must be a valid UUID");
        });
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
