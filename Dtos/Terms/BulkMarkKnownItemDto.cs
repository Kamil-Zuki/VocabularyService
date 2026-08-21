namespace VocabularyService.Dtos.Terms;

public readonly record struct BulkMarkKnownItemDto(string SurfaceText, string Type = "WORD");
