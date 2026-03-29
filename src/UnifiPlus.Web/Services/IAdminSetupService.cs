using UnifiPlus.Web.Models;

namespace UnifiPlus.Web.Services;

public interface IAdminSetupService
{
    Task<AdminSetupViewModel> BuildViewModelAsync(CancellationToken cancellationToken);

    Task SaveConfigurationAsync(AdminUniFiSetupRequest request, CancellationToken cancellationToken);

    Task<UniFiConnectionTestResult> TestConnectionAsync(AdminUniFiSetupRequest request, CancellationToken cancellationToken);
}
