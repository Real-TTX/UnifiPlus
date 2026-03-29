using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class SetClientAliasRequest
{
    [Required]
    public string ClientId { get; init; } = string.Empty;

    public string Alias { get; init; } = string.Empty;
}
