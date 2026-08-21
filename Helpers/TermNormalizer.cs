using System.Text.RegularExpressions;

namespace VocabularyService.Helpers;

/// <summary>
/// Нормализация surface-form для ключей термина (как в плане LingQ).
/// </summary>
public static class TermNormalizer
{
    private static readonly Regex WhitespaceCollapse = new(@"\s+", RegexOptions.Compiled);

    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var trimmed = input.Trim().ToLowerInvariant();
        return WhitespaceCollapse.Replace(trimmed, " ");
    }
}
