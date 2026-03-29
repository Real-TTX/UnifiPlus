using UnifiPlus.Web.Models;

namespace UnifiPlus.Web.Services;

public interface IUniFiApiClient
{
    Task<IReadOnlyList<UniFiClient>> GetClientsAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WanInterface>> GetWanInterfacesAsync(CancellationToken cancellationToken);

    Task<UniFiConnectionTestResult> TestConnectionAsync(AdminUniFiSetupRequest request, CancellationToken cancellationToken);

    Task AssignClientToUserAsync(string userId, string clientId, CancellationToken cancellationToken);

    Task UpdateWanPolicyAsync(string userId, string clientId, string wanId, CancellationToken cancellationToken);

    Task UpdateBandwidthLimitAsync(string userId, string clientId, int? downloadLimitMbps, int? uploadLimitMbps, CancellationToken cancellationToken);

    Task<IReadOnlyList<ActiveRuleViewModel>> GetActiveRulesAsync(CancellationToken cancellationToken);

    Task DeleteActiveRuleAsync(string ruleId, CancellationToken cancellationToken);

    Task<UniFiRecoverySnapshot> GetRecoverySnapshotAsync(CancellationToken cancellationToken);
}
