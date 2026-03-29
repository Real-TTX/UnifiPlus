using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class AdminUniFiSetupRequest : IValidatableObject
{
    [Required]
    [Display(Name = "UniFi base URL")]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Site name")]
    public string Site { get; set; } = "default";

    [Display(Name = "API key")]
    public string? ApiKey { get; set; }

    [Display(Name = "Username")]
    public string? Username { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [Display(Name = "Allow self-signed TLS")]
    public bool AllowSelfSignedTls { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasApiKey = !string.IsNullOrWhiteSpace(ApiKey);
        var hasUsername = !string.IsNullOrWhiteSpace(Username);
        var hasPassword = !string.IsNullOrWhiteSpace(Password);

        if (!hasApiKey && !hasUsername && !hasPassword)
        {
            yield return new ValidationResult(
                "Provide either an API key or a username and password.",
                [nameof(ApiKey), nameof(Username), nameof(Password)]);
        }

        if (!hasApiKey && hasUsername && !hasPassword)
        {
            yield return new ValidationResult(
                "Password is required when a username is provided without an API key.",
                [nameof(Password)]);
        }

        if (!hasApiKey && !hasUsername && hasPassword)
        {
            yield return new ValidationResult(
                "Username is required when a password is provided without an API key.",
                [nameof(Username)]);
        }
    }
}
