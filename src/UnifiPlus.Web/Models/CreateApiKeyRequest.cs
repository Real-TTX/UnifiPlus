using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class CreateApiKeyRequest
{
    [Required]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;
}
