using Microsoft.EntityFrameworkCore;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Domain;

namespace VocabularyService.Services;

public class NoteTypeService : INoteTypeService
{
    private readonly VocabularyServiceContext _db;

    public NoteTypeService(VocabularyServiceContext db)
    {
        _db = db;
    }

    public async Task<(NoteType Type, CardTemplate DefaultTemplate)> EnsureSentenceMiningAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.NoteTypes
            .Include(t => t.CardTemplates)
            .Include(t => t.NoteFields)
            .FirstOrDefaultAsync(
                t => t.ProjectId == projectId && t.Name == SentenceMiningNoteType.TypeName,
                cancellationToken);

        if (existing != null)
        {
            var tmpl = existing.CardTemplates.FirstOrDefault(x => x.TemplateKey == SentenceMiningNoteType.DefaultTemplateKey)
                       ?? throw new InvalidOperationException("Sentence Mining note type missing default card template.");
            return (existing, tmpl);
        }

        var now = DateTime.UtcNow;
        var typeId = Guid.NewGuid();
        var noteType = new NoteType
        {
            Id = typeId,
            ProjectId = projectId,
            Name = SentenceMiningNoteType.TypeName,
            Version = 1,
            Css = null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var fieldRows = new[]
        {
            (SentenceMiningNoteType.Expression, "Expression", "textarea", 0),
            (SentenceMiningNoteType.Word, "Word", "text", 1),
            (SentenceMiningNoteType.Translation, "Translation", "textarea", 2),
            (SentenceMiningNoteType.Transcription, "Transcription", "text", 3),
            (SentenceMiningNoteType.WordTypes, "Word types", "text", 4),
            (SentenceMiningNoteType.Definition, "Definition", "textarea", 5),
            (SentenceMiningNoteType.Example, "Example / context", "textarea", 6),
            (SentenceMiningNoteType.Synonyms, "Synonyms", "tags", 7),
            (SentenceMiningNoteType.Antonyms, "Antonyms", "textarea", 8),
            (SentenceMiningNoteType.Notes, "Notes", "textarea", 9),
            (SentenceMiningNoteType.SourceTitle, "Source title", "text", 10),
            (SentenceMiningNoteType.SourceUrl, "Source URL", "url", 11),
            (SentenceMiningNoteType.Image, "Image", "image", 12),
            (SentenceMiningNoteType.Audio, "Audio", "audio", 13),
        };

        foreach (var (key, label, ftype, order) in fieldRows)
        {
            noteType.NoteFields.Add(new NoteField
            {
                Id = Guid.NewGuid(),
                NoteTypeId = typeId,
                FieldKey = key,
                Label = label,
                FieldType = ftype,
                SortOrder = order,
                Required = key is SentenceMiningNoteType.Expression or SentenceMiningNoteType.Word or SentenceMiningNoteType.Translation,
                Archived = false,
                ConfigJson = null,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        var template = new CardTemplate
        {
            Id = Guid.NewGuid(),
            NoteTypeId = typeId,
            TemplateKey = SentenceMiningNoteType.DefaultTemplateKey,
            Name = "Default",
            FrontTemplate = SentenceMiningNoteType.DefaultFrontTemplate,
            BackTemplate = SentenceMiningNoteType.DefaultBackTemplate,
            SortOrder = 0,
            Enabled = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        noteType.CardTemplates.Add(template);

        _db.NoteTypes.Add(noteType);
        await _db.SaveChangesAsync(cancellationToken);

        return (noteType, template);
    }

    public async Task<NoteType> GetSentenceMiningForEditorAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var owns = await _db.Projects
            .AnyAsync(p => p.Id == projectId && p.UserId == userId, cancellationToken);
        if (!owns)
            throw new UnauthorizedAccessException("Project not found or access denied");

        await EnsureSentenceMiningAsync(projectId, cancellationToken).ConfigureAwait(false);

        return await _db.NoteTypes
            .AsNoTracking()
            .Include(nt => nt.NoteFields)
            .Include(nt => nt.CardTemplates)
            .FirstAsync(
                nt => nt.ProjectId == projectId && nt.Name == SentenceMiningNoteType.TypeName,
                cancellationToken);
    }
}
