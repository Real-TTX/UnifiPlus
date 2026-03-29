using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class LoginViewModel
{
    [Required]
    [Display(Name = "User ID")]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
