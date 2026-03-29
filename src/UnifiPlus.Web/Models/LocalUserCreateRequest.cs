using System.ComponentModel.DataAnnotations;
using UnifiPlus.Web.Authorization;

namespace UnifiPlus.Web.Models;

public sealed class LocalUserCreateRequest
{
    [Required]
    [Display(Name = "User ID")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Repeat password")]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = AppRoles.User;
}
