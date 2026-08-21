namespace VocabularyService.Options;

public class BillingOptions
{
    public const string SectionName = "Billing";

    public string GrpcAddress { get; set; } = "http://localhost:5127";
}
