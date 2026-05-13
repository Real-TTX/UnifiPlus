using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class AdminAssignClientRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;
}
