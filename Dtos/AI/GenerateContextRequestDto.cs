namespace VocabularyService.Dtos.AI;

public class GenerateContextRequestDto
{
    public string TargetWord { get; set; } = null!;
    public string Language { get; set; } = null!; // ISO 639-1
    public string UserLevel { get; set; } = null!; // CEFR: A1, A2, B1, B2, C1, C2
    public int Count { get; set; } = 3; // Количество примеров (обычно 3-5)
}
