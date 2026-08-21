using AutoMapper;
using FluentValidation;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Pvs.Content.Grpc;
using VocabularyService.Data;
using VocabularyService.Data.Entities;
using VocabularyService.Dtos.Analytics;
using VocabularyService.Helpers;
using VocabularyService.Services;
using static Pvs.Content.Grpc.AnalyticsService;

namespace VocabularyService.Grpc;

/// <summary>
/// gRPC сервис для аналитики
/// </summary>
public class AnalyticsGrpcService : AnalyticsServiceBase
{
    private readonly ILogger<AnalyticsGrpcService> _logger;
    private readonly IAnalyticsService _analyticsService;
    private readonly IMapper _mapper;
    private readonly IHostEnvironment _env;
    private readonly IValidator<GetVocabularyStatsRequest> _vocabularyStatsValidator;
    private readonly IValidator<GetHeatmapRequest> _heatmapValidator;
    private readonly IValidator<GetDailySummaryRequest> _dailySummaryValidator;
    private readonly VocabularyService.Data.VocabularyServiceContext _dbContext;

    public AnalyticsGrpcService(
        ILogger<AnalyticsGrpcService> logger,
        IAnalyticsService analyticsService,
        IMapper mapper,
        IHostEnvironment env,
        IValidator<GetVocabularyStatsRequest> vocabularyStatsValidator,
        IValidator<GetHeatmapRequest> heatmapValidator,
        IValidator<GetDailySummaryRequest> dailySummaryValidator,
        VocabularyService.Data.VocabularyServiceContext dbContext)
    {
        _logger = logger;
        _analyticsService = analyticsService;
        _mapper = mapper;
        _env = env;
        _vocabularyStatsValidator = vocabularyStatsValidator;
        _heatmapValidator = heatmapValidator;
        _dailySummaryValidator = dailySummaryValidator;
        _dbContext = dbContext;
    }

    //===== SR-ANL-01: Оценка словарного запаса =====
    public override async Task<GetVocabularyStatsResponse> GetVocabularyStats(
        GetVocabularyStatsRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        var validationResult = await _vocabularyStatsValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        if (!Guid.TryParse(request.ProjectId, out var projectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Project ID format"));
        }

        try
        {
            var statsDto = await _analyticsService.GetVocabularyStatsAsync(
                userId,
                projectId,
                context.CancellationToken);

            return _mapper.Map<GetVocabularyStatsResponse>(statsDto);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vocabulary stats");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== SR-ANL-02: Календарь активности =====
    public override async Task<GetHeatmapResponse> GetHeatmap(
        GetHeatmapRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        var validationResult = await _heatmapValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        Guid? projectId = null;
        if (!string.IsNullOrEmpty(request.ProjectId))
        {
            if (!Guid.TryParse(request.ProjectId, out var parsedProjectId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Project ID format"));
            }
            projectId = parsedProjectId;
        }

        try
        {
            var heatmapDto = await _analyticsService.GetHeatmapAsync(
                userId,
                projectId,
                request.Year,
                context.CancellationToken);

            return _mapper.Map<GetHeatmapResponse>(heatmapDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting heatmap");
            var detail = _env.IsDevelopment() ? $"{ex.Message}" : "Internal server error";
            throw new RpcException(new Status(StatusCode.Internal, detail));
        }
    }

    //===== SR-ANL-03: Дневная сводка =====
    public override async Task<GetDailySummaryResponse> GetDailySummary(
        GetDailySummaryRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        var validationResult = await _dailySummaryValidator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, validationResult.ToString()));
        }

        int? timezoneOffset = null;
        if (request.TimezoneOffset != null && request.TimezoneOffset.HasValue)
        {
            timezoneOffset = request.TimezoneOffset.Value;
        }

        try
        {
            var summaryDto = await _analyticsService.GetDailySummaryAsync(
                userId,
                timezoneOffset,
                context.CancellationToken);

            return _mapper.Map<GetDailySummaryResponse>(summaryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily summary");
            var detail = _env.IsDevelopment() ? $"{ex.Message}" : "Internal server error";
            throw new RpcException(new Status(StatusCode.Internal, detail));
        }
    }

    //===== Phase 2: Оценка баланса навыков =====
    public override async Task<GetSkillBalanceResponse> GetSkillBalance(
        GetSkillBalanceRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);
        
        if (!Guid.TryParse(request.ProjectId, out var projectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ProjectId"));
        }

        try
        {
            var stats = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                System.Linq.Queryable.Select(
                    System.Linq.Queryable.GroupBy(
                        System.Linq.Queryable.Where(
                            _dbContext.UserTermStatuses,
                            ts => ts.UserId == userId && ts.ProjectId == projectId && ts.Status == "SAVED"
                        ),
                        ts => 1
                    ),
                    g => new
                    {
                        AvgR = g.Average(ts => (double)ts.ReadingLevel),
                        AvgL = g.Average(ts => (double)ts.ListeningLevel),
                        AvgW = g.Average(ts => (double)ts.WritingLevel),
                        AvgS = g.Average(ts => (double)ts.SpeakingLevel)
                    }
                ),
                context.CancellationToken);

            return new GetSkillBalanceResponse
            {
                ProjectId = request.ProjectId,
                AverageReadingLevel = stats == null ? 0 : (int)Math.Round(stats.AvgR),
                AverageListeningLevel = stats == null ? 0 : (int)Math.Round(stats.AvgL),
                AverageWritingLevel = stats == null ? 0 : (int)Math.Round(stats.AvgW),
                AverageSpeakingLevel = stats == null ? 0 : (int)Math.Round(stats.AvgS)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skill balance");
            var detail = _env.IsDevelopment() ? $"{ex.Message}" : "Internal server error";
            throw new RpcException(new Status(StatusCode.Internal, detail));
        }
    }

    //===== Phase 4: Autopilot =====
    public override async Task<GetDailyAutopilotPlanResponse> GetDailyAutopilotPlan(
        GetDailyAutopilotPlanRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.ProjectId, out var projectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ProjectId"));
        }

        try
        {
            var autopilotService = context.GetHttpContext().RequestServices.GetRequiredService<IAutopilotService>();
            return await autopilotService.GetDailyPlanAsync(userId, projectId, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily autopilot plan");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Phase 3: Skill Assessment History =====
    public override async Task<GetSkillAssessmentHistoryResponse> GetSkillAssessmentHistory(
        GetSkillAssessmentHistoryRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.ProjectId, out var projectId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ProjectId"));
        }

        int limit = request.Limit > 0 ? request.Limit : 20;

        try
        {
            var logs = await _dbContext.SkillAssessmentLogs
                .AsNoTracking()
                .Where(l => l.UserId == userId && l.ProjectId == projectId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(limit)
                .ToListAsync(context.CancellationToken);

            var response = new GetSkillAssessmentHistoryResponse();
            response.Logs.AddRange(logs.Select(l => new SkillAssessmentLogDto
            {
                Id = l.Id.ToString(),
                Skill = l.Skill,
                Score = l.Score,
                CreatedAt = l.CreatedAt.ToString("O")
            }));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skill assessment history");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    //===== Phase 4: Skill Activity Tracking =====
    public override async Task<TrackSkillActivityResponse> TrackSkillActivity(
        TrackSkillActivityRequest request,
        ServerCallContext context)
    {
        var userId = GrpcContextHelper.GetUserId(context);

        if (!Guid.TryParse(request.ProjectId, out var projectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ProjectId"));

        if (request.SkillTypeId <= 0 || request.Value <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "SkillTypeId and Value must be positive"));

        var db = context.GetHttpContext().RequestServices.GetRequiredService<VocabularyServiceContext>();

        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var now = DateTime.UtcNow;

            // Atomic upsert: accumulate value, never overwrite
            await db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO internal.""UserSkillActivities"" (""Id"", ""UserId"", ""ProjectId"", ""Date"", ""SkillTypeId"", ""Value"", ""CreatedAt"", ""UpdatedAt"")
                  VALUES (uuid_generate_v4(), {0}, {1}, {2}, {3}, {4}, {5}, {5})
                  ON CONFLICT (""UserId"", ""ProjectId"", ""Date"", ""SkillTypeId"")
                  DO UPDATE SET ""Value"" = ""UserSkillActivities"".""Value"" + excluded.""Value"",
                               ""UpdatedAt"" = excluded.""UpdatedAt""",
                new object[] { userId, projectId, today, request.SkillTypeId, request.Value, now });

            // Read back the current total and threshold
            var activity = await db.UserSkillActivities
                .AsNoTracking()
                .Include(a => a.SkillType)
                .FirstOrDefaultAsync(
                    a => a.UserId == userId && a.ProjectId == projectId
                         && a.Date == today && a.SkillTypeId == request.SkillTypeId,
                    context.CancellationToken);

            var totalValue = activity?.Value ?? request.Value;
            var threshold = activity?.SkillType?.CompletionThreshold ?? int.MaxValue;

            return new TrackSkillActivityResponse
            {
                TotalValueToday = totalValue,
                IsCompleted = totalValue >= threshold
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking skill activity for user {UserId}, skill {SkillTypeId}", userId, request.SkillTypeId);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
