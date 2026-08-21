using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class ExplainGrammarRequestValidator : AbstractValidator<ExplainGrammarRequest>
{
    public ExplainGrammarRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("User ID must be a valid UUID");

        RuleFor(x => x.Sentence)
            .NotEmpty()
            .WithMessage("Sentence cannot be empty")
            .MaximumLength(1000)
            .WithMessage("Sentence cannot exceed 1000 characters");

        RuleFor(x => x.TargetWord)
            .NotEmpty()
            .WithMessage("Target word cannot be empty")
            .MaximumLength(100)
            .WithMessage("Target word cannot exceed 100 characters");

        RuleFor(x => x.UserNativeLanguage)
            .NotEmpty()
            .WithMessage("User native language code cannot be empty")
            .Length(2)
            .WithMessage("User native language code must be 2 characters (ISO 639-1)");
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
