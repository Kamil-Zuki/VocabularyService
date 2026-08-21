namespace VocabularyService.Domain;

/// <summary>Stable keys for the default "Sentence Mining" note type (Anki-like field registry).</summary>
public static class SentenceMiningNoteType
{
    public const string TypeName = "Sentence Mining";

    public const string DefaultTemplateKey = "default";

    public const string Expression = "Expression";
    public const string Word = "Word";
    public const string Translation = "Translation";
    public const string Transcription = "Transcription";
    public const string WordTypes = "WordTypes";
    public const string Definition = "Definition";
    public const string Example = "Example";
    public const string Synonyms = "Synonyms";
    public const string Antonyms = "Antonyms";
    public const string Notes = "Notes";
    public const string SourceTitle = "SourceTitle";
    public const string SourceUrl = "SourceUrl";
    public const string Image = "Image";
    public const string Audio = "Audio";

    /// <summary>Default Anki-style templates using {{FieldKey}} placeholders.</summary>
    public const string DefaultFrontTemplate = "{{Expression}}";

    public const string DefaultBackTemplate =
        "{{Word}}\n\n{{Translation}}\n\n{{Definition}}\n\n{{Example}}\n\n{{Synonyms}}\n\n{{Antonyms}}\n\n{{Notes}}";
}
