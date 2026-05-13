using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class AdminSetClientAliasRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;
}
