using UnifiPlus.Web.Models;

namespace UnifiPlus.Web.Services;

public interface IBandwidthTemplateStore
{
    string StoragePath { get; }

    Task<BandwidthTemplateSettings> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(BandwidthTemplateSettings settings, CancellationToken cancellationToken);
}
