namespace VocabularyService.Options;

public class MediaOptions
{
    public const string SectionName = "Media";

    public string GrpcAddress { get; set; } = "http://localhost:5121";
}
