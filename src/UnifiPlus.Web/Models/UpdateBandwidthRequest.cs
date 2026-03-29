using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class UpdateBandwidthRequest
{
    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Range(1, 100000)]
    public int? DownloadLimitMbps { get; init; }

    [Range(1, 100000)]
    public int? UploadLimitMbps { get; init; }
}
