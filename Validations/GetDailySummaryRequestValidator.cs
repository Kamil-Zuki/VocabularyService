using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class GetDailySummaryRequestValidator : AbstractValidator<GetDailySummaryRequest>
{
    public GetDailySummaryRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("User ID must be a valid UUID");

        // TimezoneOffset is optional Int32Value, validation is handled in gRPC service
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
