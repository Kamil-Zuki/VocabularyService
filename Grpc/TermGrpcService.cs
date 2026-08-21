using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Pvs.Content.Grpc;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Data.Entities.JsonTypes;
using VocabularyService.Helpers;
using VocabularyService.Services;
using static Pvs.Content.Grpc.TermService;

namespace VocabularyService.Grpc;

public class TermGrpcService : TermServiceBase
{
    private readonly ITermService _terms;
    private readonly VocabularyServiceContext _db;
    private readonly ILogger<TermGrpcService> _logger;

    public TermGrpcService(
        ITermService terms,
        VocabularyServiceContext db,
        ILogger<TermGrpcService> logger)
    {
        _terms = terms;
        _db = db;
        _logger = logger;
    }

    public override async Task<TermDetailsResponse> CreateOrUpdateTerm(
        CreateOrUpdateTermRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid project id"));

        try
        {
            var row = await _terms.CreateOrUpdateAsync(
                userId,
                projectId,
                request.TermText,
                request.Type,
                string.IsNullOrWhiteSpace(request.Language) ? null : request.Language,
                string.IsNullOrWhiteSpace(request.Status) ? null : request.Status,
                string.IsNullOrWhiteSpace(request.Meaning) ? null : request.Meaning,
                string.IsNullOrWhiteSpace(request.FirstSentence) ? null : request.FirstSentence,
                string.IsNullOrWhiteSpace(request.FirstSourceTitle) ? null : request.FirstSourceTitle,
                string.IsNullOrWhiteSpace(request.FirstSourceUrl) ? null : request.FirstSourceUrl,
                context.CancellationToken);

            var term = await _db.ProjectTerms.AsNoTracking().FirstAsync(t => t.Id == row.ProjectTermId, context.CancellationToken);
            return await BuildDetailsAsync(userId, term, row, context.CancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateOrUpdateTerm failed");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<TermDetailsResponse> MarkTermKnown(TermActionRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid project id"));

        try
        {
            var row = await _terms.MarkKnownAsync(
                userId,
                projectId,
                request.TermText,
                request.Type,
                string.IsNullOrWhiteSpace(request.Language) ? null : request.Language,
                context.CancellationToken);

            var term = await _db.ProjectTerms.AsNoTracking().FirstAsync(t => t.Id == row.ProjectTermId, context.CancellationToken);
            return await BuildDetailsAsync(userId, term, row, context.CancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarkTermKnown failed");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<TermDetailsResponse> IgnoreTerm(TermActionRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid project id"));

        try
        {
            var row = await _terms.IgnoreAsync(
                userId,
                projectId,
                request.TermText,
                request.Type,
                string.IsNullOrWhiteSpace(request.Language) ? null : request.Language,
                context.CancellationToken);

            var term = await _db.ProjectTerms.AsNoTracking().FirstAsync(t => t.Id == row.ProjectTermId, context.CancellationToken);
            return await BuildDetailsAsync(userId, term, row, context.CancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IgnoreTerm failed");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<BulkMarkKnownResponse> BulkMarkKnown(BulkMarkKnownRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid project id"));

        try
        {
            var items = request.Items.Count > 0
                ? request.Items
                    .Select(i => new Dtos.Terms.BulkMarkKnownItemDto(i.TermText, i.Type))
                    .ToList()
                : request.TermTexts
                    .Select(t => new Dtos.Terms.BulkMarkKnownItemDto(t, "WORD"))
                    .ToList();

            var n = await _terms.BulkMarkKnownAsync(
                userId,
                projectId,
                items,
                string.IsNullOrWhiteSpace(request.Language) ? null : request.Language,
                context.CancellationToken);

            return new BulkMarkKnownResponse { UpdatedCount = n };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BulkMarkKnown failed");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<TermDetailsResponse> GetTermDetails(GetTermDetailsRequest request, ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid project id"));

        try
        {
            var (term, row) = await _terms.GetDetailsAsync(
                userId,
                projectId,
                request.TermText,
                request.Type,
                context.CancellationToken);

            return await BuildDetailsAsync(userId, term, row, context.CancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTermDetails failed");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<SearchTermDuplicatesResponse> SearchTermDuplicates(
        SearchTermDuplicatesRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid project id"));

        try
        {
            var (term, cards) = await _terms.SearchDuplicatesAsync(
                userId,
                projectId,
                request.TermText,
                request.Type,
                context.CancellationToken);

            var norm = TermNormalizer.Normalize(request.TermText);
            var response = new SearchTermDuplicatesResponse
            {
                NormalizedText = norm,
                IsDuplicate = cards.Count > 0 || term != null,
            };

            if (term != null)
            {
                var uts = await _db.UserTermStatuses.AsNoTracking().FirstOrDefaultAsync(
                    r => r.UserId == userId && r.ProjectTermId == term.Id,
                    context.CancellationToken);
                response.MatchingTerms.Add(await BuildDetailsAsync(userId, term, uts, context.CancellationToken));
            }

            foreach (var c in cards)
            {
                var progress = c.UserCardProgresses.FirstOrDefault();
                response.ExistingCards.Add(ToCardPreview(c, progress, c.Deck));
            }

            return response;
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchTermDuplicates failed");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<ListProjectTermsResponse> ListProjectTerms(
        ListProjectTermsRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid project id"));

        try
        {
            var (rows, totalCount) = await _terms.ListProjectTermsAsync(
                userId,
                projectId,
                string.IsNullOrWhiteSpace(request.Status) ? null : request.Status,
                string.IsNullOrWhiteSpace(request.Type) ? null : request.Type,
                string.IsNullOrWhiteSpace(request.Q) ? null : request.Q,
                request.PageNumber,
                request.PageSize,
                context.CancellationToken);

            var resp = new ListProjectTermsResponse();
            foreach (var r in rows)
            {
                var item = new ProjectTermListItem
                {
                    TermId = r.TermId.ToString("D"),
                    Text = r.Text,
                    NormalizedText = r.NormalizedText,
                    Type = r.Type,
                    Language = r.Language,
                    Status = TermApiStatusFormatter.ToClientStatus(r.DbStatus),
                    UpdatedAt = Timestamp.FromDateTime(r.UpdatedAtUtc),
                    RelatedCardCount = r.RelatedCardCount,
                    ReadingLevel = r.ReadingLevel,
                    ListeningLevel = r.ListeningLevel,
                    WritingLevel = r.WritingLevel,
                    SpeakingLevel = r.SpeakingLevel,
                };

                if (!string.IsNullOrEmpty(r.Meaning))
                    item.Meaning = r.Meaning;

                if (!string.IsNullOrEmpty(r.FirstSentence))
                    item.FirstSentence = r.FirstSentence;

                if (!string.IsNullOrEmpty(r.FirstSourceTitle))
                    item.FirstSourceTitle = r.FirstSourceTitle;

                if (!string.IsNullOrEmpty(r.FirstSourceUrl))
                    item.FirstSourceUrl = r.FirstSourceUrl;

                resp.Items.Add(item);
            }

            resp.TotalCount = totalCount;

            return resp;
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (FormatException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListProjectTerms failed");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<PurgeDemoImportResponse> PurgeDemoImport(
        PurgeDemoImportRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid project id"));

        try
        {
            var result = await _terms.PurgeDemoImportDataAsync(
                userId,
                projectId,
                context.CancellationToken);

            return new PurgeDemoImportResponse
            {
                CardsDeleted = result.CardsDeleted,
                StatusesDeleted = result.StatusesDeleted,
                TermsDeleted = result.TermsDeleted,
            };
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PurgeDemoImport failed");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    private async Task<TermDetailsResponse> BuildDetailsAsync(
        Guid userId,
        ProjectTerm term,
        UserTermStatus? row,
        CancellationToken ct)
    {
        // Media — свойство с JSON conversion, не FK; Include только для навигаций
        var related = await _db.Cards
            .AsNoTracking()
            .Include(c => c.Deck)
            .Include(c => c.Note)
            .Include(c => c.UserCardProgresses.Where(p => p.UserId == userId))
            .Where(c => c.ProjectTermId == term.Id && c.CreatorId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(12)
            .ToListAsync(ct);

        var resp = new TermDetailsResponse
        {
            TermId = term.Id.ToString(),
            ProjectId = term.ProjectId.ToString(),
            TermText = term.Text,
            NormalizedText = term.NormalizedText,
            Type = term.Type,
            Language = term.Language ?? "",
            Status = row?.Status ?? "NEW",
            ReadingLevel = row?.ReadingLevel ?? 0,
            ListeningLevel = row?.ListeningLevel ?? 0,
            WritingLevel = row?.WritingLevel ?? 0,
            SpeakingLevel = row?.SpeakingLevel ?? 0,
        };

        if (!string.IsNullOrEmpty(row?.Meaning))
            resp.Meaning = row.Meaning;

        if (!string.IsNullOrEmpty(row?.FirstSentence))
            resp.FirstSentence = row.FirstSentence;

        if (!string.IsNullOrEmpty(row?.FirstSourceTitle))
            resp.FirstSourceTitle = row.FirstSourceTitle;

        if (!string.IsNullOrEmpty(row?.FirstSourceUrl))
            resp.FirstSourceUrl = row.FirstSourceUrl;

        foreach (var c in related)
        {
            var progress = c.UserCardProgresses.FirstOrDefault();
            resp.RelatedCards.Add(ToCardPreview(c, progress, c.Deck));
        }

        return resp;
    }

    private static CardPreview ToCardPreview(Card c, UserCardProgress? progress, Deck deck)
    {
        var m = c.Note?.FieldValues ?? new Dictionary<string, NoteFieldValue>();
        var media = NoteFieldMapHelper.BuildCardMedia(m);
        var preview = new CardPreview
        {
            Id = c.Id.ToString(),
            SrsStatus = MapSrsFromProgress(progress),
            HasAudio = media?.AudioId.HasValue == true || !string.IsNullOrEmpty(media?.AudioUrl),
            DeckTitle = deck.Title,
        };
        if (c.Note != null)
        {
            preview.Note = new NotePayload
            {
                Id = c.Note.Id.ToString(),
                NoteTypeId = c.Note.NoteTypeId.ToString(),
            };
            if (c.Note.ProjectTermId.HasValue)
                preview.Note.ProjectTermId = c.Note.ProjectTermId.Value.ToString();
            foreach (var kv in c.Note.FieldValues)
            {
                var p = new NoteFieldValuePayload();
                if (!string.IsNullOrEmpty(kv.Value.String))
                    p.StringValue = kv.Value.String;
                if (kv.Value.Strings is { Count: > 0 })
                    p.StringValues.AddRange(kv.Value.Strings);
                preview.Note.FieldValues[kv.Key] = p;
            }
        }

        return preview;
    }

    /// <summary>Согласовано с <see cref="CardGrpcService"/> SRS-маппингом по строковому статусу.</summary>
    private static SrsStatus MapSrsStatus(string status) =>
        status.Trim().ToUpperInvariant() switch
        {
            "NEW" => SrsStatus.New,
            "LEARNING" => SrsStatus.Learning,
            "REVIEW" => SrsStatus.Review,
            "RELEARNING" => SrsStatus.Relearning,
            "MATURE" => SrsStatus.Mature,
            _ => SrsStatus.New
        };

    /// <summary>Копия правил из CardService.MapProgressState для превью.</summary>
    private static SrsStatus MapSrsFromProgress(UserCardProgress? progress)
    {
        if (progress == null)
            return SrsStatus.New;

        var label = progress.State switch
        {
            0 => "NEW",
            1 => "LEARNING",
            2 when progress.Due >= DateTime.UtcNow.AddDays(21) => "MATURE",
            2 => "REVIEW",
            3 or 4 => "RELEARNING",
            _ => "NEW"
        };
        return MapSrsStatus(label);
    }
}
