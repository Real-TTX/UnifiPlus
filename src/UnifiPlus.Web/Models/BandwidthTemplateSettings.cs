namespace UnifiPlus.Web.Models;

public sealed class BandwidthTemplateSettings
{
    public List<int> DownloadTemplatesMbps { get; init; } = [50, 100, 200];

    public List<int> UploadTemplatesMbps { get; init; } = [5, 10, 20];

    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
