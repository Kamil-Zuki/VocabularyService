using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

public class BatchSubmitReviewsRequestValidator : AbstractValidator<BatchSubmitReviewsRequest>
{
    public BatchSubmitReviewsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("User ID must be a valid UUID");

        RuleFor(x => x.Reviews)
            .NotNull()
            .WithMessage("Reviews list cannot be null")
            .Must(reviews => reviews != null && reviews.Count > 0)
            .WithMessage("At least one review is required")
            .Must(reviews => reviews != null && reviews.Count <= 1000)
            .WithMessage("Maximum 1000 reviews per batch");

        RuleForEach(x => x.Reviews)
            .SetValidator(new BatchReviewItemValidator());
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}

public class BatchReviewItemValidator : AbstractValidator<BatchReviewItem>
{
    public BatchReviewItemValidator()
    {
        RuleFor(x => x.CardId)
            .NotEmpty()
            .Must(BeValidGuid)
            .WithMessage("Card ID must be a valid UUID");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 4)
            .WithMessage("Rating must be between 1 and 4");

        RuleFor(x => x.ReviewedAt)
            .NotNull()
            .WithMessage("ReviewedAt timestamp is required")
            .Must(reviewedAt => reviewedAt != null && reviewedAt.ToDateTime() <= DateTime.UtcNow.AddHours(1))
            .WithMessage("ReviewedAt cannot be more than 1 hour in the future");

        RuleFor(x => x.DurationMs)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Duration must be non-negative")
            .LessThanOrEqualTo(300000) // 5 minutes max
            .WithMessage("Duration cannot exceed 5 minutes");

        RuleFor(x => x.SessionId)
            .Must((item, value) => 
                string.IsNullOrEmpty(value) || Guid.TryParse(value, out _))
            .WithMessage("Session ID must be a valid UUID if provided");
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
