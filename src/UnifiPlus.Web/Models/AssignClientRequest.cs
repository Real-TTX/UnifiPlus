using System.ComponentModel.DataAnnotations;

namespace UnifiPlus.Web.Models;

public sealed class AssignClientRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;
}
