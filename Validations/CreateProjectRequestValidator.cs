using FluentValidation;
using Pvs.Content.Grpc;

namespace VocabularyService.Validations;

/// <summary>
/// Валидатор для CreateProjectRequest
/// </summary>
public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    /// <summary>
    /// Список валидных ISO 639-1 кодов языков (основные)
    /// </summary>
    private static readonly HashSet<string> ValidLanguageCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "ru", "ja", "zh", "es", "fr", "de", "it", "pt", "ko", "ar", "hi", "tr", "pl", "nl", "sv", "da", "fi", "no", "cs", "ro", "hu", "el", "he", "th", "vi", "id", "uk", "bg", "hr", "sk", "sl", "et", "lv", "lt", "mt", "ga", "cy"
    };

    public CreateProjectRequestValidator()
    {
        // Валидация Title
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title cannot be empty")
            .MaximumLength(100)
            .WithMessage("Title cannot exceed 100 characters")
            .MinimumLength(1)
            .WithMessage("Title must be at least 1 character");

        // Валидация SourceLang
        RuleFor(x => x.SourceLang)
            .NotEmpty()
            .WithMessage("Source language code is required")
            .Length(2)
            .WithMessage("Source language code must be exactly 2 characters (ISO 639-1)")
            .Must(BeValidLanguageCode)
            .WithMessage("Source language code must be a valid ISO 639-1 code");

        // Валидация TargetLang
        RuleFor(x => x.TargetLang)
            .NotEmpty()
            .WithMessage("Target language code is required")
            .Length(2)
            .WithMessage("Target language code must be exactly 2 characters (ISO 639-1)")
            .Must(BeValidLanguageCode)
            .WithMessage("Target language code must be a valid ISO 639-1 code");

        // Валидация: SourceLang и TargetLang должны отличаться
        RuleFor(x => x)
            .Must(x => !string.Equals(x.SourceLang, x.TargetLang, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Source language and target language must be different");

        // Валидация Settings (если переданы)
        When(x => x.Settings != null, () =>
        {
            RuleFor(x => x.Settings!.RequestRetention)
                .InclusiveBetween(0.0, 1.0)
                .WithMessage("Request retention must be between 0.0 and 1.0");

            RuleFor(x => x.Settings!.MaximumInterval)
                .GreaterThan(0)
                .WithMessage("Maximum interval must be greater than 0")
                .LessThanOrEqualTo(36500)
                .WithMessage("Maximum interval cannot exceed 36500 days");

            RuleFor(x => x.Settings!.W)
                .Must(w => w == null || w.Count == 0 || w.Count == 18)
                .WithMessage("FSRS weights array must contain exactly 18 values or be empty");
        });
    }

    /// <summary>
    /// Проверяет, является ли код валидным ISO 639-1 кодом
    /// </summary>
    private static bool BeValidLanguageCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        // Проверка формата: только буквы, длина 2
        if (code.Length != 2 || !code.All(char.IsLetter))
            return false;

        // Проверка наличия в списке валидных кодов
        return ValidLanguageCodes.Contains(code);
    }
}

