using System.Text.Json.Serialization;
namespace VocabularyService.Data.Entities.JsonTypes
{

    // 1. Projects Table
    public class FsrsSettings
    {
        [JsonPropertyName("request_retention")]
        public double RequestRetention { get; set; } = 0.9;
        [JsonPropertyName("maximum_interval")]
        public int MaximumInterval { get; set; } = 36500;
        [JsonPropertyName("w")]
        public double[] W { get; set; } = [];

        /// <summary>Шаги learning в секундах (как в Anki: по умолчанию 1 мин и 10 мин = 60, 600). Пусто = дефолт py-fsrs.</summary>
        [JsonPropertyName("learning_steps_seconds")]
        public int[]? LearningStepsSeconds { get; set; }

        /// <summary>Шаги relearning в секундах (Anki по умолчанию часто 10 мин = 600). Пусто = дефолт py-fsrs.</summary>
        [JsonPropertyName("relearning_steps_seconds")]
        public int[]? RelearningStepsSeconds { get; set; }

        /// <summary>Случайный разброс интервала (Anki: обычно вкл.). null после десериализации старого JSON = как true.</summary>
        [JsonPropertyName("enable_fuzzing")]
        public bool? EnableFuzzing { get; set; }
    }

    public class TtsSettings
    {
        [JsonPropertyName("voice_name")]
        public string? VoiceName { get; set; }

        [JsonPropertyName("rate")]
        public double Rate { get; set; } = 1.0;

        [JsonPropertyName("pitch")]
        public double Pitch { get; set; } = 1.0;
    }

    public class ProjectStats
    {
        [JsonPropertyName("total_lemmas")]
        public int TotalLemmas { get; set; }
        [JsonPropertyName("mature_lemmas")]
        public int MatureLemmas { get; set; }
    }

    // 2. Cards Table
    public class TargetIndex
    {
        [JsonPropertyName("start")]
        public int Start { get; set; }
        [JsonPropertyName("len")]
        public int Len { get; set; }
    }

    public class SourceMeta
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
        [JsonPropertyName("time")]
        public int? Time { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>Optional dictionary-style fields (editor / browser extension parity).</summary>
    public class CardLexiconFields
    {
        [JsonPropertyName("transcription")]
        public string? Transcription { get; set; }
        [JsonPropertyName("word_types")]
        public string? WordTypes { get; set; }
        [JsonPropertyName("definition")]
        public string? Definition { get; set; }
        [JsonPropertyName("example")]
        public string? Example { get; set; }
        [JsonPropertyName("antonyms")]
        public string? Antonyms { get; set; }
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class CardMedia
    {
        [JsonPropertyName("audio_id")]
        public Guid? AudioId { get; set; }
        [JsonPropertyName("image_id")]
        public Guid? ImageId { get; set; }
        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
        [JsonPropertyName("audio_url")]
        public string? AudioUrl { get; set; }
    }

    // 3. AuthorProfiles Table
    public class SocialLinks
    {
        [JsonPropertyName("youtube")]
        public string? Youtube { get; set; }
        [JsonPropertyName("website")]
        public string? Website { get; set; }
    }

    public class AuthorStatsCache
    {
        [JsonPropertyName("rating")]
        public double Rating { get; set; }
        [JsonPropertyName("students")]
        public int Students { get; set; }
    }

    // 4. Contributions Table — mirrors note field map for proposed changes
    public class ContributionPayload
    {
        [JsonPropertyName("field_values")]
        public Dictionary<string, NoteFieldValue> FieldValues { get; set; } = new();
    }

    /// <summary>Single field slot in a note's <see cref="NoteFieldValue"/> map (Anki-like).</summary>
    public class NoteFieldValue
    {
        [JsonPropertyName("string")]
        public string? String { get; set; }

        [JsonPropertyName("strings")]
        public List<string>? Strings { get; set; }
    }
}
