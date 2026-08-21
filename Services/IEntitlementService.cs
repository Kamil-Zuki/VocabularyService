using VocabularyService.Dtos.Community;

namespace VocabularyService.Services;

/// <summary>
/// Интерфейс сервиса для управления правами доступа
/// </summary>
public interface IEntitlementService
{
    /// <summary>
    /// Проверяет наличие права доступа к колоде
    /// </summary>
    Task<EntitlementDto> CheckEntitlementAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Предоставляет право доступа
    /// </summary>
    Task GrantEntitlementAsync(
        Guid userId,
        Guid deckId,
        Guid? productId,
        string source,
        string? externalOrderId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Отзывает право доступа
    /// </summary>
    Task RevokeEntitlementAsync(
        Guid userId,
        Guid deckId,
        CancellationToken cancellationToken = default);
}
