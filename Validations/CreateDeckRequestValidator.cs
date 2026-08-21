using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

/// <summary>
/// Валидатор для CreateDeckRequest
/// </summary>
public class CreateDeckRequestValidator : AbstractValidator<CreateDeckRequest>
{
    public CreateDeckRequestValidator()
    {
        // Валидация Title
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title cannot be empty")
            .MinimumLength(3)
            .WithMessage("Title must be at least 3 characters")
            .MaximumLength(100)
            .WithMessage("Title cannot exceed 100 characters");

        // Валидация ProjectId
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("Project ID is required")
            .Must(BeValidGuid)
            .WithMessage("Project ID must be a valid UUID");

        // Валидация ParentDeckId (если указан) выполняется в сервисе
        // Здесь валидируем только обязательные поля
    }

    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}
