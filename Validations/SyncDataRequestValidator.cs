using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class SyncDataRequestValidator : AbstractValidator<SyncDataRequest>
{
    public SyncDataRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("User ID must be a valid UUID");

        RuleFor(x => x.ProjectId)
            .Must((request, value) => 
                string.IsNullOrEmpty(value) || Guid.TryParse(value, out _))
            .WithMessage("Project ID must be a valid UUID if provided");

        // LastSyncToken is optional, but if provided should be valid
        RuleFor(x => x.LastSyncToken)
            .Must(token => token == null || token.ToDateTime() <= DateTime.UtcNow.AddDays(1))
            .WithMessage("Last sync token cannot be in the future");
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
