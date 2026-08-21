namespace VocabularyService.Dtos.Text;

public enum TokenStatus
{
    New = 0,      // Новое слово (не в словаре)
    Learning = 1, // Слово в процессе изучения / LingQ
    Known = 2,    // Выученное слово
    Ignored = 3,  // Пользователь исключил форму из подсветки «новых»
}

public enum TokenType
{
    Word = 0,        // Слово
    Space = 1,       // Пробел
    Punctuation = 2 // Знак препинания
}

public class TextTokenDto
{
    public string Text { get; set; } = null!;

    /// <summary>Нормализованная реальная форма (ключ статуса в LingQ).</summary>
    public string? TermText { get; set; }

    /// <summary>Устаревающее поле; не использовать для статусов в reader.</summary>
    public string? Lemma { get; set; }

    public TokenStatus Status { get; set; }
    public TokenType Type { get; set; }

    /// <summary>ProjectTerm id when this surface is tracked for the user.</summary>
    public Guid? ProjectTermId { get; set; }
}
