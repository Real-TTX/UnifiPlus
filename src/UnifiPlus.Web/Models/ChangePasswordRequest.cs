using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class ChangePasswordRequest
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

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
