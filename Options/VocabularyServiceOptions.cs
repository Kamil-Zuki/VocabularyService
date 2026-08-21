namespace VocabularyService.Options;

/// <summary>
/// Конфигурация для VocabularyService
/// </summary>
public class VocabularyServiceOptions
{
    public const string SectionName = "VocabularyService";

    /// <summary>
    /// Максимальное количество проектов на пользователя
    /// </summary>
    public int MaxProjectsPerUser { get; set; } = 50;

    /// <summary>
    /// Пресеты настроек FSRS для разных языков
    /// </summary>
    public Dictionary<string, FsrsPreset> FsrsPresets { get; set; } = new();

    /// <summary>
    /// Название системной колоды "Inbox"
    /// </summary>
    public string InboxDeckTitle { get; set; } = "Inbox";
}

/// <summary>
/// Пресет настроек FSRS
/// </summary>
public class FsrsPreset
{
    /// <summary>
    /// Request retention (0.0 - 1.0)
    /// </summary>
    public double RequestRetention { get; set; } = 0.9;

    /// <summary>
    /// Maximum interval в днях
    /// </summary>
    public int MaximumInterval { get; set; } = 36500;

    /// <summary>
    /// Веса FSRS (18 значений)
    /// </summary>
    public double[] W { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Включить краткосрочную память
    /// </summary>
    public bool EnableShortTerm { get; set; } = true;

    /// <summary>Шаги learning (секунды), как в пресете колоды Anki.</summary>
    public int[]? LearningStepsSeconds { get; set; }

    /// <summary>Шаги relearning (секунды).</summary>
    public int[]? RelearningStepsSeconds { get; set; }

    /// <summary>Fuzz интервалов (как в Anki FSRS).</summary>
    public bool? EnableFuzzing { get; set; }
}

