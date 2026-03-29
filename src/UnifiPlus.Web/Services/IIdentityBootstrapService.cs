namespace UnifiPlus.Web.Services;

public interface IIdentityBootstrapService
{
    Task<bool> IsAdminSetupRequiredAsync(CancellationToken cancellationToken);

    Task<bool> IsUniFiConnectionSetupRequiredAsync(CancellationToken cancellationToken);

    Task<Data.LocalUser> CreateInitialAdminAsync(string userId, string password, CancellationToken cancellationToken);
}
