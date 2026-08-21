using System.Text.RegularExpressions;
using Google.Protobuf.Collections;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Domain;
using JsonNote = VocabularyService.Data.Entities.JsonTypes;
namespace VocabularyService.Helpers;

/// <summary>Read/write Sentence Mining field map (<see cref="SentenceMiningNoteType"/> keys).</summary>
public static class NoteFieldMapHelper
{
    public static string GetString(IReadOnlyDictionary<string, NoteFieldValue> m, string key) =>
        m.TryGetValue(key, out var v) ? (v.String ?? string.Empty) : string.Empty;

    public static List<string> GetStringList(IReadOnlyDictionary<string, NoteFieldValue> m, string key) =>
        m.TryGetValue(key, out var v) && v.Strings is { Count: > 0 } ? v.Strings : [];

    public static string GetExpression(IReadOnlyDictionary<string, NoteFieldValue> m) =>
        GetString(m, SentenceMiningNoteType.Expression);

    public static string GetWord(IReadOnlyDictionary<string, NoteFieldValue> m) =>
        GetString(m, SentenceMiningNoteType.Word);

    public static string GetTranslation(IReadOnlyDictionary<string, NoteFieldValue> m) =>
        GetString(m, SentenceMiningNoteType.Translation);

    /// <summary>Text for PostgreSQL full-text search (card row).</summary>
    public static string BuildSearchDocument(IReadOnlyDictionary<string, NoteFieldValue> m)
    {
        var synStr = GetStringList(m, SentenceMiningNoteType.Synonyms);
        var parts = new[]
        {
            GetExpression(m),
            GetWord(m),
            GetTranslation(m),
            GetString(m, SentenceMiningNoteType.Definition),
            synStr.Count > 0 ? string.Join(' ', synStr) : string.Empty,
        };
        return string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
    }

    public static JsonNote.TargetIndex CalculateTargetIndex(string expression, string word)
    {
        var surface = word.Trim();
        if (string.IsNullOrEmpty(surface))
            throw new ArgumentException("Word is empty.");

        // Prefer whole-word match (case-insensitive) so "deas" does not match inside "Ideas".
        var pattern = @"\b" + Regex.Escape(surface) + @"\b";
        var m = Regex.Match(expression, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
            return new JsonNote.TargetIndex { Start = m.Index, Len = m.Length };

        // Phrases / scripts where \b is unreliable: require non-letter/digit boundaries.
        var index = expression.IndexOf(surface, StringComparison.Ordinal);
        if (index < 0)
            index = expression.IndexOf(surface, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            throw new ArgumentException($"Target word '{surface}' not found in expression.");

        static bool IsWordChar(char c) =>
            char.IsLetterOrDigit(c) || c == '_' || c == '\'';

        var boundaryBefore = index == 0 || !IsWordChar(expression[index - 1]);
        var end = index + surface.Length;
        var boundaryAfter = end >= expression.Length || !IsWordChar(expression[end]);
        if (boundaryBefore && boundaryAfter)
            return new JsonNote.TargetIndex { Start = index, Len = surface.Length };

        throw new ArgumentException(
            $"Target word '{surface}' is not aligned to word boundaries in expression.");
    }

    public static JsonNote.CardMedia? BuildCardMedia(IReadOnlyDictionary<string, NoteFieldValue> m)
    {
        var img = GetString(m, SentenceMiningNoteType.Image).Trim();
        var aud = GetString(m, SentenceMiningNoteType.Audio).Trim();
        Guid? imageId = null;
        Guid? audioId = null;
        if (Guid.TryParse(img, out var gi)) imageId = gi;
        if (Guid.TryParse(aud, out var ga)) audioId = ga;
        var hasUrl = img.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                     aud.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        if (imageId == null && audioId == null && !hasUrl && string.IsNullOrEmpty(img) && string.IsNullOrEmpty(aud))
            return null;

        return new JsonNote.CardMedia
        {
            ImageId = imageId,
            AudioId = audioId,
            ImageUrl = imageId == null && (img.StartsWith("http", StringComparison.OrdinalIgnoreCase) || img.Contains('/')) ? img : null,
            AudioUrl = audioId == null && (aud.StartsWith("http", StringComparison.OrdinalIgnoreCase) || aud.Contains('/')) ? aud : null,
        };
    }

    public static bool HasAudio(IReadOnlyDictionary<string, NoteFieldValue>? fieldValues)
    {
        if (fieldValues is null) return false;
        var m = BuildCardMedia(fieldValues);
        return m is not null && (m.AudioId.HasValue || !string.IsNullOrEmpty(m.AudioUrl));
    }

    public static bool HasImage(IReadOnlyDictionary<string, NoteFieldValue>? fieldValues)
    {
        if (fieldValues is null) return false;
        var m = BuildCardMedia(fieldValues);
        return m is not null && (m.ImageId.HasValue || !string.IsNullOrEmpty(m.ImageUrl));
    }

    /// <summary>Normalize create/update map: ensure Sentence Mining keys exist with defaults.</summary>
    public static Dictionary<string, NoteFieldValue> NormalizeSentenceMiningMap(
        IReadOnlyDictionary<string, NoteFieldValue>? input)
    {
        var map = input != null
            ? new Dictionary<string, NoteFieldValue>(input, StringComparer.Ordinal)
            : new Dictionary<string, NoteFieldValue>(StringComparer.Ordinal);

        void SetIfEmpty(string key, string value = "")
        {
            if (!map.ContainsKey(key))
                map[key] = new NoteFieldValue { String = value };
        }

        SetIfEmpty(SentenceMiningNoteType.Expression);
        SetIfEmpty(SentenceMiningNoteType.Word);
        SetIfEmpty(SentenceMiningNoteType.Translation);
        SetIfEmpty(SentenceMiningNoteType.Transcription);
        SetIfEmpty(SentenceMiningNoteType.WordTypes);
        SetIfEmpty(SentenceMiningNoteType.Definition);
        SetIfEmpty(SentenceMiningNoteType.Example);
        if (!map.ContainsKey(SentenceMiningNoteType.Synonyms))
            map[SentenceMiningNoteType.Synonyms] = new NoteFieldValue { Strings = [] };
        SetIfEmpty(SentenceMiningNoteType.Antonyms);
        SetIfEmpty(SentenceMiningNoteType.Notes);
        SetIfEmpty(SentenceMiningNoteType.SourceTitle);
        SetIfEmpty(SentenceMiningNoteType.SourceUrl);
        SetIfEmpty(SentenceMiningNoteType.Image);
        SetIfEmpty(SentenceMiningNoteType.Audio);

        return map;
    }

    /// <summary>Merge patch into existing note field values (update).</summary>
    public static void MergeInto(Note note, IReadOnlyDictionary<string, NoteFieldValue> patch)
    {
        foreach (var kv in patch)
            note.FieldValues[kv.Key] = kv.Value;
        note.UpdatedAt = DateTime.UtcNow;
    }

    public static Dictionary<string, NoteFieldValue> FromProtoMap(MapField<string, Pvs.Content.Grpc.NoteFieldValuePayload> map)
    {
        var d = new Dictionary<string, NoteFieldValue>(StringComparer.Ordinal);
        foreach (var kv in map)
        {
            var v = new NoteFieldValue();
            if (!string.IsNullOrEmpty(kv.Value.StringValue))
                v.String = kv.Value.StringValue;
            if (kv.Value.StringValues.Count > 0)
                v.Strings = [.. kv.Value.StringValues];
            d[kv.Key] = v;
        }

        return d;
    }
}
