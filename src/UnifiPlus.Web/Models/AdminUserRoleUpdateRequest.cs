using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class AdminUserRoleUpdateRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}
