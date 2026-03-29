using Microsoft.AspNetCore.Identity;
using UnifiPlus.Web.Authorization;
using UnifiPlus.Web.Data;

namespace UnifiPlus.Web.Services;

public sealed class IdentityBootstrapService : IIdentityBootstrapService
{
    private readonly ILocalUserStore _localUserStore;
    private readonly IUniFiConfigurationStore _configurationStore;
    private readonly PasswordHasher<LocalUser> _passwordHasher = new();

    public IdentityBootstrapService(ILocalUserStore localUserStore, IUniFiConfigurationStore configurationStore)
    {
        _localUserStore = localUserStore;
        _configurationStore = configurationStore;
    }

    public async Task<bool> IsAdminSetupRequiredAsync(CancellationToken cancellationToken)
    {
        return !await _localUserStore.HasAnyUsersAsync(cancellationToken);
    }

    public async Task<bool> IsUniFiConnectionSetupRequiredAsync(CancellationToken cancellationToken)
    {
        var configuration = await _configurationStore.GetAsync(cancellationToken);
        return configuration is null;
    }

    public async Task<LocalUser> CreateInitialAdminAsync(string userId, string password, CancellationToken cancellationToken)
    {
        var users = await _localUserStore.GetAllAsync(cancellationToken);
        if (users.Count > 0)
        {
            throw new InvalidOperationException("Initial administrator can only be created when no users exist yet.");
        }

        var admin = new LocalUser
        {
            UserId = userId.Trim(),
            Role = AppRoles.Admin,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        admin.PasswordHash = _passwordHasher.HashPassword(admin, password);
        await _localUserStore.SaveAllAsync([admin], cancellationToken);
        return admin;
    }
}
