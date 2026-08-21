using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class AnalyzeTextRequestValidator : AbstractValidator<AnalyzeTextRequest>
{
    public AnalyzeTextRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("User ID must be a valid UUID");

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("Project ID must be a valid UUID");

        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("Text cannot be empty")
            .MaximumLength(100000) // 100KB максимум
            .WithMessage("Text cannot exceed 100,000 characters");
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
