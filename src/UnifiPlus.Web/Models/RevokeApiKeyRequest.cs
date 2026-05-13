using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class RevokeApiKeyRequest
{
    [Required]
    public string KeyId { get; set; } = string.Empty;
}
