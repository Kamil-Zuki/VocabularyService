using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class CaptureCardRequestValidator : AbstractValidator<CaptureCardRequest>
{
    public CaptureCardRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("Project ID must be a valid UUID");

        RuleFor(x => x.FieldValues)
            .NotEmpty();

        RuleFor(x => x)
            .Must(HasValidMiningFields)
            .WithMessage("field_values must include non-empty Expression (3–1000 chars) and Word (≤100 chars).");

        RuleFor(x => x)
            .Must(x => string.IsNullOrWhiteSpace(x.DeckId) || Guid.TryParse(x.DeckId.Trim(), out _))
            .WithMessage("deck_id must be a valid UUID when provided.");
    }

    private static bool HasValidMiningFields(CaptureCardRequest x)
    {
        var expr = GetString(x, "Expression");
        var word = GetString(x, "Word");
        if (string.IsNullOrWhiteSpace(expr) || expr.Length < 3 || expr.Length > 1000)
            return false;
        if (string.IsNullOrWhiteSpace(word) || word.Length > 100)
            return false;
        return true;
    }

    private static string? GetString(CaptureCardRequest r, string key) =>
        r.FieldValues.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v.StringValue)
            ? v.StringValue
            : null;

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}
