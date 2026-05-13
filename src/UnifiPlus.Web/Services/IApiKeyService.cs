using UnifiPlus.Web.Data;

namespace UnifiPlus.Web.Services;

public interface IApiKeyService
{
    Task<(ApiKeyRecord Record, string PlaintextKey)> CreateAsync(string userId, string name, CancellationToken cancellationToken);

    Task<IReadOnlyList<ApiKeyRecord>> GetForUserAsync(string userId, CancellationToken cancellationToken);

    Task<bool> RevokeAsync(string userId, string keyId, CancellationToken cancellationToken);

    Task<LocalUser?> ValidateAsync(string rawKey, CancellationToken cancellationToken);
}
