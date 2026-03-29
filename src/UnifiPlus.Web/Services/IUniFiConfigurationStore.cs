using UnifiPlus.Web.Data;

namespace UnifiPlus.Web.Services;

public interface IUniFiConfigurationStore
{
    string StoragePath { get; }

    Task<StoredUniFiConfiguration?> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(StoredUniFiConfiguration configuration, CancellationToken cancellationToken);
}
