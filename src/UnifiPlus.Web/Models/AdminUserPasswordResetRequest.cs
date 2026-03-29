using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class AdminUserPasswordResetRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Repeat new password")]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
