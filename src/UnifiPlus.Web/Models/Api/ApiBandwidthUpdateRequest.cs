using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models.Api;

public sealed class ApiBandwidthUpdateRequest
{
    [Range(1, 100000)]
    public int? DownloadLimitMbps { get; init; }

    [Range(1, 100000)]
    public int? UploadLimitMbps { get; init; }
}
