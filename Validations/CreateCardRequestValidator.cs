using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class CreateCardRequestValidator : AbstractValidator<CreateCardRequest>
{
    public CreateCardRequestValidator()
    {
        RuleFor(x => x.DeckId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("Deck ID must be a valid UUID");

        RuleFor(x => x.FieldValues)
            .NotEmpty()
            .WithMessage("field_values is required");

        RuleFor(x => x)
            .Must(HasValidMiningFields)
            .WithMessage("field_values must include non-empty Expression (3–1000 chars) and Word (≤100 chars).");
    }

    private static bool HasValidMiningFields(CreateCardRequest x)
    {
        var expr = GetString(x, "Expression");
        var word = GetString(x, "Word");
        if (string.IsNullOrWhiteSpace(expr) || expr.Length < 3 || expr.Length > 1000)
            return false;
        if (string.IsNullOrWhiteSpace(word) || word.Length > 100)
            return false;
        return true;
    }

    private static string? GetString(CreateCardRequest r, string key) =>
        r.FieldValues.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v.StringValue)
            ? v.StringValue
            : null;

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}
