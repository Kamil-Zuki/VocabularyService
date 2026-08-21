public record UserUsageStatsDto(
    int ProjectsUsed,
    int CardsUsed,
    int AiRequestsTodayUsed,
    int BooksUsed);

public interface IBillingLimitService
{
    Task<int> GetMaxProjectsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> GetMaxCardsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> GetCurrentCardCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> CanCreateProjectAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> CanCreateCardAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> CanUseAiAsync(Guid userId, CancellationToken cancellationToken = default);

    Task RecordAiRequestAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserUsageStatsDto> GetUserUsageStatsAsync(Guid userId, CancellationToken cancellationToken = default);
}
