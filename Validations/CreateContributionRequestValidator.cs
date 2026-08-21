using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class CreateContributionRequestValidator : AbstractValidator<CreateContributionRequest>
{
    public CreateContributionRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.DeckId)
            .NotEmpty()
            .WithMessage("Deck ID is required");

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => t == "EDIT" || t == "ADD" || t == "DELETE")
            .WithMessage("Type must be EDIT, ADD, or DELETE");

        When(x => x.Type == "EDIT" || x.Type == "DELETE", () =>
        {
            RuleFor(x => x.CardId)
                .NotEmpty()
                .WithMessage("Card ID is required for EDIT and DELETE types");
        });

        When(x => x.Type == "EDIT" || x.Type == "ADD", () =>
        {
            RuleFor(x => x.Content)
                .NotNull()
                .WithMessage("Content is required for EDIT and ADD types");

            RuleFor(x => x.Content!.FieldValues)
                .NotEmpty()
                .When(x => x.Content != null)
                .WithMessage("field_values is required");

            RuleFor(x => x)
                .Must(r =>
                    r.Content != null
                    && r.Content.FieldValues.Count > 0
                    && TryGetString(r.Content, "Expression", out var e) && !string.IsNullOrWhiteSpace(e)
                    && TryGetString(r.Content, "Word", out var w) && !string.IsNullOrWhiteSpace(w))
                .When(x => x.Content != null)
                .WithMessage("Expression and Word must be set in field_values.");
        });
    }

    private static bool TryGetString(CardContent content, string key, out string value)
    {
        value = string.Empty;
        if (!content.FieldValues.TryGetValue(key, out var p) || string.IsNullOrEmpty(p.StringValue))
            return false;
        value = p.StringValue;
        return true;
    }
}
