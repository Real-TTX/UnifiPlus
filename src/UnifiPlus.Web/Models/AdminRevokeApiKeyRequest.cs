using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class AdminRevokeApiKeyRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string KeyId { get; set; } = string.Empty;
}
