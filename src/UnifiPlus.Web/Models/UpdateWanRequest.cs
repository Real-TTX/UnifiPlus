using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class UpdateWanRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string WanId { get; set; } = string.Empty;
}
