using Grpc.Core;
using Pvs.Billing.Grpc;
using static Pvs.Billing.Grpc.BillingService;

namespace VocabularyService.Services;

public class BillingEntitlementClient : IBillingEntitlementClient
{
    private readonly BillingServiceClient _grpcClient;
    private readonly ILogger<BillingEntitlementClient> _logger;

    public BillingEntitlementClient(
        BillingServiceClient grpcClient,
        ILogger<BillingEntitlementClient> logger)
    {
        _grpcClient = grpcClient;
        _logger = logger;
    }

    public async Task<BillingEntitlements> GetEntitlementsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _grpcClient.GetEntitlementsAsync(
                new GetEntitlementsRequest { UserId = userId.ToString() },
                cancellationToken: cancellationToken);

            return new BillingEntitlements(
                response.PlanCode,
                response.Entitlements.ToDictionary(
                    x => x.Key,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase));
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "BillingService unavailable for user {UserId}, falling back to free limits", userId);
            return BillingEntitlementClient.FallbackFree();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling BillingService for user {UserId}, falling back to free limits", userId);
            return BillingEntitlementClient.FallbackFree();
        }
    }

    public static BillingEntitlements FallbackFree()
    {
        return new BillingEntitlements(
            "free",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["maxProjects"] = "3",
                ["maxCards"] = "500",
                ["aiRequestsPerDay"] = "10"
            });
    }
}
