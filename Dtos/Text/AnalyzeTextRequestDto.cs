namespace VocabularyService.Dtos.Text;

public class AnalyzeTextRequestDto
{
    public Guid ProjectId { get; set; }
    public string Text { get; set; } = null!;
}
