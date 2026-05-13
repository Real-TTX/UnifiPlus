using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models.Api;

public sealed class ApiUplinkUpdateRequest
{
    [Required]
    public string WanId { get; init; } = string.Empty;
}
