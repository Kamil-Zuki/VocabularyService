using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VocabularyService.Data;
using VocabularyService.Domain;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Dtos;
using VocabularyService.Dtos.Cards;

namespace VocabularyService.Services;

public class ImportService : IImportService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImportService> _logger;

    public ImportService(IServiceProvider serviceProvider, ILogger<ImportService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Guid> CreateJobAsync(Guid userId, Guid deckId, Guid projectId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VocabularyServiceContext>();
        
        var job = new Data.Entities.ImportJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeckId = deckId,
            ProjectId = projectId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.ImportJobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);
        return job.Id;
    }

    public async Task<Data.Entities.ImportJob?> GetJobAsync(Guid jobId, Guid userId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VocabularyServiceContext>();
        return await context.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId, cancellationToken);
    }

    public async Task ProcessImportJobAsync(Guid jobId, string documentId, string fileName, string configJson, CancellationToken cancellationToken = default)
    {
        // This runs in a background thread, so we create a new scope
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VocabularyServiceContext>();
        var mediaService = scope.ServiceProvider.GetRequiredService<IMediaService>();
        var cardService = scope.ServiceProvider.GetRequiredService<ICardService>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        var job = await context.ImportJobs.FindAsync(new object[] { jobId }, cancellationToken);
        if (job == null) return;

        try
        {
            job.Status = "RUNNING";
            await context.SaveChangesAsync(cancellationToken);

            var docGuid = Guid.Parse(documentId);
            var url = await mediaService.GetDocumentUrlAsync(docGuid, cancellationToken);
            using var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var filePath = Path.Combine(tempDir, fileName);
                await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs, cancellationToken);
                }

                if (fileName.EndsWith(".apkg", StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessAnkiPackageAsync(filePath, tempDir, job, configJson, mediaService, cardService, context, cancellationToken);
                }
                else if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessCsvPackageAsync(filePath, fileName.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase), job, configJson, cardService, context, cancellationToken);
                }

                job.Status = "COMPLETED";
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing import job {JobId}", jobId);
            job.Status = "FAILED";
            job.ErrorMessage = ex.Message;
        }

        await context.SaveChangesAsync(CancellationToken.None); // save even if cancelled
    }

    private async Task ProcessAnkiPackageAsync(string zipPath, string extractPath, Data.Entities.ImportJob job, string configJson, IMediaService mediaService, ICardService cardService, VocabularyServiceContext context, CancellationToken cancellationToken)
    {
        ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);

        var mediaJsonPath = Path.Combine(extractPath, "media");
        var mediaMap = new Dictionary<string, string>();
        if (File.Exists(mediaJsonPath))
        {
            var mediaJson = await File.ReadAllTextAsync(mediaJsonPath, cancellationToken);
            mediaMap = JsonSerializer.Deserialize<Dictionary<string, string>>(mediaJson) ?? new Dictionary<string, string>();
        }

        // Upload media files to MediaService and map to Polyraspad UUIDs
        var uploadedMedia = new Dictionary<string, Guid>(); // AnkiFilename -> PolyraspadId
        foreach (var kvp in mediaMap)
        {
            var ankiKey = kvp.Key;
            var originalFilename = kvp.Value;
            var filePath = Path.Combine(extractPath, ankiKey);

            if (File.Exists(filePath))
            {
                await using var fs = File.OpenRead(filePath);
                var ext = Path.GetExtension(originalFilename).ToLowerInvariant();
                var contentType = "application/octet-stream";
                if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";
                else if (ext == ".png") contentType = "image/png";
                else if (ext == ".mp3") contentType = "audio/mpeg";

                try
                {
                    if (contentType.StartsWith("image/"))
                    {
                        var id = await mediaService.UploadImageAsync(fs, contentType, cancellationToken);
                        uploadedMedia[originalFilename] = id;
                    }
                    else if (contentType.StartsWith("audio/"))
                    {
                        var id = await mediaService.UploadAudioAsync(fs, contentType, cancellationToken);
                        uploadedMedia[originalFilename] = id;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to upload media file {Filename}", originalFilename);
                }
            }
        }

        var dbPath = Path.Combine(extractPath, "collection.anki2");
        if (!File.Exists(dbPath))
            throw new FileNotFoundException("collection.anki2 not found in .apkg");

        var dtos = new List<CreateCardDto>();

        using (var connection = new SqliteConnection($"Data Source={dbPath}"))
        {
            await connection.OpenAsync(cancellationToken);

            var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM notes";
            job.TotalRows = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
            await context.SaveChangesAsync(cancellationToken);

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT flds FROM notes";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var flds = reader.GetString(0);
                var fields = flds.Split('\x1f'); // Anki field separator

                var map = new Dictionary<string, NoteFieldValue>();
                
                // Extremely simplified mapping: field 0 -> expression, field 1 -> meaning. 
                // In reality, this should be parsed using configJson mapping.
                if (fields.Length > 0) map[SentenceMiningNoteType.Expression] = new NoteFieldValue { String = ExtractTextWithoutMedia(fields[0]) };
                if (fields.Length > 1) map[SentenceMiningNoteType.Translation] = new NoteFieldValue { String = ExtractTextWithoutMedia(fields[1]) };

                // Try to find media in fields
                foreach (var f in fields)
                {
                    foreach (var (ankiName, polyId) in uploadedMedia)
                    {
                        if (f.Contains(ankiName))
                        {
                            if (ankiName.EndsWith(".jpg") || ankiName.EndsWith(".png") || ankiName.EndsWith(".jpeg"))
                                map[SentenceMiningNoteType.Image] = new NoteFieldValue { String = polyId.ToString() };
                            else if (ankiName.EndsWith(".mp3"))
                                map[SentenceMiningNoteType.Audio] = new NoteFieldValue { String = polyId.ToString() };
                        }
                    }
                }

                dtos.Add(new CreateCardDto
                {
                    UserId = job.UserId,
                    DeckId = job.DeckId,
                    FieldValues = map
                });

                if (dtos.Count >= 100)
                {
                    await cardService.BulkCreateCardsAsync(job.UserId, job.DeckId, dtos, cancellationToken);
                    job.ProcessedRows += dtos.Count;
                    await context.SaveChangesAsync(cancellationToken);
                    dtos.Clear();
                }
            }
        }

        if (dtos.Count > 0)
        {
            await cardService.BulkCreateCardsAsync(job.UserId, job.DeckId, dtos, cancellationToken);
            job.ProcessedRows += dtos.Count;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private string ExtractTextWithoutMedia(string input)
    {
        // Simplistic strip of [sound:...] and <img> tags, keeping just text for expression/meaning
        // Very basic approach.
        return System.Text.RegularExpressions.Regex.Replace(input, @"\[sound:.*?\]|<img.*?>", string.Empty).Trim();
    }

    private async Task ProcessCsvPackageAsync(string filePath, bool isTsv, Data.Entities.ImportJob job, string configJson, ICardService cardService, VocabularyServiceContext context, CancellationToken cancellationToken)
    {
        var config = JsonSerializer.Deserialize<ImportConfigJson>(configJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (config == null || config.Mapping == null)
            throw new Exception("Invalid config JSON");

        using var reader = new StreamReader(filePath);
        var csvConfig = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false, // Preview on frontend doesn't skip header, we import all rows
            Delimiter = isTsv ? "\t" : ",",
            BadDataFound = null,
            MissingFieldFound = null
        };
        
        using var csv = new CsvHelper.CsvReader(reader, csvConfig);
        
        var dtos = new List<CreateCardDto>();
        while (await csv.ReadAsync())
        {
            var map = new Dictionary<string, NoteFieldValue>();

            if (config.Mapping.Sentence >= 0 && csv.TryGetField<string>(config.Mapping.Sentence, out var expression) && !string.IsNullOrWhiteSpace(expression))
                map[SentenceMiningNoteType.Expression] = new NoteFieldValue { String = expression.Trim() };
            
            if (config.Mapping.Translation >= 0 && csv.TryGetField<string>(config.Mapping.Translation, out var meaning) && !string.IsNullOrWhiteSpace(meaning))
                map[SentenceMiningNoteType.Translation] = new NoteFieldValue { String = meaning.Trim() };

            if (config.Mapping.Target >= 0 && csv.TryGetField<string>(config.Mapping.Target, out var target) && !string.IsNullOrWhiteSpace(target))
                map[SentenceMiningNoteType.Word] = new NoteFieldValue { String = target.Trim() };

            if (map.Count > 0)
            {
                dtos.Add(new CreateCardDto
                {
                    UserId = job.UserId,
                    DeckId = job.DeckId,
                    FieldValues = map
                });
            }

            if (dtos.Count >= 100)
            {
                await cardService.BulkCreateCardsAsync(job.UserId, job.DeckId, dtos, cancellationToken);
                job.ProcessedRows += dtos.Count;
                await context.SaveChangesAsync(cancellationToken);
                dtos.Clear();
            }
        }

        if (dtos.Count > 0)
        {
            await cardService.BulkCreateCardsAsync(job.UserId, job.DeckId, dtos, cancellationToken);
            job.ProcessedRows += dtos.Count;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public class ImportConfigJson
    {
        public Guid DeckId { get; set; }
        public ImportColumnMapping? Mapping { get; set; }
        public string? DuplicateStrategy { get; set; }
    }

    public class ImportColumnMapping
    {
        public int Sentence { get; set; }
        public int Translation { get; set; }
        public int Target { get; set; }
    }
}
