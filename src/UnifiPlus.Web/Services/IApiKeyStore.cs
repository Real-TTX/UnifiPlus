using UnifiPlus.Web.Data;

namespace UnifiPlus.Web.Services;

public interface IApiKeyStore
{
    Task<IReadOnlyList<ApiKeyRecord>> GetAllAsync(CancellationToken cancellationToken);

    Task SaveAllAsync(IReadOnlyList<ApiKeyRecord> apiKeys, CancellationToken cancellationToken);
}
