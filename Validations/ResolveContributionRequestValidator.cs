using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class ResolveContributionRequestValidator : AbstractValidator<ResolveContributionRequest>
{
    public ResolveContributionRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.ContributionId)
            .NotEmpty()
            .WithMessage("Contribution ID is required");

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => s == "MERGED" || s == "REJECTED")
            .WithMessage("Status must be MERGED or REJECTED");
    }
}
