using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class AdminUpdateManagedWanRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string WanId { get; set; } = string.Empty;
}
