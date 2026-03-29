namespace UnifiPlus.Web.Data;

public sealed class LocalUser
{
    public string UserId { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public Dictionary<string, string> ClientAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
