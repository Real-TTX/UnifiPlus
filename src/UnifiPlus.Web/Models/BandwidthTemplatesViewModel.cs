namespace UnifiPlus.Web.Models;

public sealed class BandwidthTemplatesViewModel
{
    public IReadOnlyList<int> DownloadTemplatesMbps { get; init; } = [50, 100, 200];

    public IReadOnlyList<int> UploadTemplatesMbps { get; init; } = [5, 10, 20];

    public string StoragePath { get; init; } = string.Empty;

    public DateTimeOffset? LastUpdatedUtc { get; init; }

    public string? StatusMessage { get; init; }

    public bool StatusIsSuccess { get; init; }
}
