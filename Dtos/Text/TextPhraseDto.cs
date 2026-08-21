namespace VocabularyService.Dtos.Text;

public class TextPhraseDto
{
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public string Text { get; set; } = null!;
    public TokenStatus Status { get; set; }
    public Guid? ProjectTermId { get; set; }
}
