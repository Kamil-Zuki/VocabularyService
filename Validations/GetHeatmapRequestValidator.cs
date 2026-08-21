using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class GetHeatmapRequestValidator : AbstractValidator<GetHeatmapRequest>
{
    public GetHeatmapRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("User ID must be a valid UUID");

        RuleFor(x => x.ProjectId)
            .Must((request, value) => 
                string.IsNullOrEmpty(value) || Guid.TryParse(value, out _))
            .WithMessage("Project ID must be a valid UUID if provided");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Year must be between 2000 and 2100");
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }

    private static bool BeValidGuidNullable(string? value)
    {
        return string.IsNullOrEmpty(value) || Guid.TryParse(value, out _);
    }
}
