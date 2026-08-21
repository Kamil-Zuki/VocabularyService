namespace VocabularyService.Services;

public interface IBillingEntitlementClient
{
    Task<BillingEntitlements> GetEntitlementsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public record BillingEntitlements(
    string PlanCode,
    IReadOnlyDictionary<string, string> Entitlements)
{
    public int GetInt(string key, int defaultValue)
    {
        return Entitlements.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : defaultValue;
    }
}
