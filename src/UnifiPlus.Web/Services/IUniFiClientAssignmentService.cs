using System.Security.Claims;
using UnifiPlus.Web.Models;

namespace UnifiPlus.Web.Services;

public interface IUniFiClientAssignmentService
{
    Task<DashboardViewModel> BuildDashboardAsync(ClaimsPrincipal user, CancellationToken cancellationToken);

    Task<AccountViewModel> BuildAccountAsync(ClaimsPrincipal user, CancellationToken cancellationToken);

    Task AssignClientAsync(ClaimsPrincipal user, string clientId, CancellationToken cancellationToken);

    Task UpdateWanAsync(ClaimsPrincipal user, string clientId, int wanProfile, CancellationToken cancellationToken);

    Task UpdateWanAsync(ClaimsPrincipal user, string clientId, string wanId, CancellationToken cancellationToken);

    Task UpdateBandwidthAsync(ClaimsPrincipal user, string clientId, int? downloadLimitMbps, int? uploadLimitMbps, CancellationToken cancellationToken);
}
