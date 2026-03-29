using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class CreateAdminRequest
{
    [Required]
    [Display(Name = "Administrator username")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
