using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class GenerateContextRequestValidator : AbstractValidator<GenerateContextRequest>
{
    private static readonly HashSet<string> ValidCefrLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "A1", "A2", "B1", "B2", "C1", "C2"
    };

    public GenerateContextRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("User ID must be a valid UUID");

        RuleFor(x => x.TargetWord)
            .NotEmpty()
            .WithMessage("Target word cannot be empty")
            .MaximumLength(100)
            .WithMessage("Target word cannot exceed 100 characters");

        RuleFor(x => x.Language)
            .NotEmpty()
            .WithMessage("Language code cannot be empty")
            .Length(2)
            .WithMessage("Language code must be 2 characters (ISO 639-1)");

        RuleFor(x => x.UserLevel)
            .NotEmpty()
            .WithMessage("User level cannot be empty")
            .Must(level => ValidCefrLevels.Contains(level))
            .WithMessage("User level must be one of: A1, A2, B1, B2, C1, C2");

        RuleFor(x => x.Count)
            .InclusiveBetween(1, 10)
            .WithMessage("Count must be between 1 and 10");
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
