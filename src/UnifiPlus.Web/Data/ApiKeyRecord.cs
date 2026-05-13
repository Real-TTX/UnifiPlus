namespace UnifiPlus.Web.Data;

public sealed class ApiKeyRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = string.Empty;

    public string KeyHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastUsedUtc { get; set; }

    public DateTimeOffset? RevokedUtc { get; set; }

    public bool IsRevoked => RevokedUtc.HasValue;
}
