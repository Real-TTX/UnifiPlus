using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class AdminUpdateManagedBandwidthRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Range(1, 100000)]
    public int? DownloadLimitMbps { get; set; }

    [Range(1, 100000)]
    public int? UploadLimitMbps { get; set; }
}
