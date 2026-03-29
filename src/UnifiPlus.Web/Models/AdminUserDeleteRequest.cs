using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class AdminUserDeleteRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
}
