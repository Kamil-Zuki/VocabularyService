using VocabularyService.Data.Entities;

namespace VocabularyService.Services;

public interface INoteTypeService
{
    /// <summary>Creates the default Sentence Mining type, fields, and card template for a project if missing.</summary>
    Task<(NoteType Type, CardTemplate DefaultTemplate)> EnsureSentenceMiningAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>Loads note type + fields + templates after verifying the user owns the project.</summary>
    Task<NoteType> GetSentenceMiningForEditorAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default);
}
