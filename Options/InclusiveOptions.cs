namespace VocabularyService.Options;

/// <summary>
/// Configuration for the inclusive FSRS gRPC service.
/// </summary>
public class InclusiveOptions
{
    public const string SectionName = "Inclusive";

    /// <summary>
    /// gRPC address for inclusive, for example http://localhost:40051 or http://inclusive:40051.
    /// </summary>
    public string GrpcAddress { get; set; } = "http://localhost:40051";
}
