using StackExchange.Redis;
using VocabularyService.Data;
using Microsoft.EntityFrameworkCore;

namespace VocabularyService.Services;

/// <summary>
/// Лимиты SaaS-тарифа из BillingService с fail-open fallback на free.
/// </summary>
public class BillingLimitService : IBillingLimitService
{
    private const string AiDailyKeyPrefix = "billing:ai:";

    private readonly IBillingEntitlementClient _billingClient;
    private readonly VocabularyServiceContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<BillingLimitService> _logger;

    public BillingLimitService(
        IBillingEntitlementClient billingClient,
        VocabularyServiceContext context,
        IConnectionMultiplexer redis,
        ILogger<BillingLimitService> logger)
    {
        _billingClient = billingClient;
        _context = context;
        _redis = redis;
        _logger = logger;
    }

    public async Task<int> GetMaxProjectsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entitlements = await _billingClient.GetEntitlementsAsync(userId, cancellationToken);
        return entitlements.GetInt("maxProjects", 3);
    }

    public async Task<int> GetMaxCardsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entitlements = await _billingClient.GetEntitlementsAsync(userId, cancellationToken);
        return entitlements.GetInt("maxCards", 500);
    }

    public async Task<int> GetCurrentCardCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Cards
            .CountAsync(c => c.CreatorId == userId, cancellationToken);
    }

    public async Task<bool> CanCreateProjectAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var maxProjects = await GetMaxProjectsAsync(userId, cancellationToken);
        var projectCount = await _context.Projects
            .CountAsync(p => p.UserId == userId && !p.IsArchived, cancellationToken);

        return projectCount < maxProjects;
    }

    public async Task<bool> CanCreateCardAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var maxCards = await GetMaxCardsAsync(userId, cancellationToken);
        var cardCount = await GetCurrentCardCountAsync(userId, cancellationToken);

        return cardCount < maxCards;
    }

    public async Task<bool> CanUseAiAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entitlements = await _billingClient.GetEntitlementsAsync(userId, cancellationToken);
        var dailyLimit = entitlements.GetInt("aiRequestsPerDay", 10);
        if (dailyLimit <= 0)
        {
            return false;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{AiDailyKeyPrefix}{userId:N}:{DateTime.UtcNow:yyyyMMdd}";
            var count = await db.StringGetAsync(key);
            var used = count.HasValue && int.TryParse(count!, out var parsed) ? parsed : 0;
            return used < dailyLimit;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for AI limit check, allowing request for user {UserId}", userId);
            return true;
        }
    }

    public async Task RecordAiRequestAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{AiDailyKeyPrefix}{userId:N}:{DateTime.UtcNow:yyyyMMdd}";
            await db.StringIncrementAsync(key);
            await db.KeyExpireAsync(key, TimeSpan.FromDays(2));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record AI usage for user {UserId}", userId);
        }

        await Task.CompletedTask;
    }

    public async Task<UserUsageStatsDto> GetUserUsageStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var projectsUsed = await _context.Projects
            .CountAsync(p => p.UserId == userId && !p.IsArchived, cancellationToken);

        var cardsUsed = await GetCurrentCardCountAsync(userId, cancellationToken);

        var aiRequestsTodayUsed = 0;
        try
        {
            var db = _redis.GetDatabase();
            var key = $"{AiDailyKeyPrefix}{userId:N}:{DateTime.UtcNow:yyyyMMdd}";
            var count = await db.StringGetAsync(key);
            if (count.HasValue && int.TryParse(count!, out var parsed))
            {
                aiRequestsTodayUsed = parsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for AI daily usage check for user {UserId}", userId);
        }

        var booksUsed = await _context.UserBookProgresses
            .Where(p => p.UserId == userId)
            .Select(p => p.BookId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new UserUsageStatsDto(projectsUsed, cardsUsed, aiRequestsTodayUsed, booksUsed);
    }
}
