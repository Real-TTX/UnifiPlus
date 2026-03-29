using Microsoft.AspNetCore.Identity;
using UnifiPlus.Web.Authorization;
using UnifiPlus.Web.Data;

namespace UnifiPlus.Web.Services;

public sealed class LocalIdentityService : ILocalIdentityService
{
    private readonly ILocalUserStore _localUserStore;
    private readonly PasswordHasher<LocalUser> _passwordHasher = new();

    public LocalIdentityService(ILocalUserStore localUserStore)
    {
        _localUserStore = localUserStore;
    }

    public async Task<LocalUser?> ValidateAsync(string userId, string password, CancellationToken cancellationToken)
    {
        var user = await _localUserStore.FindByUserIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }
}
