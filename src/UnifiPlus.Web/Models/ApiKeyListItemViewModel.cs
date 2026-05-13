namespace UnifiPlus.Web.Models;

public sealed class ApiKeyListItemViewModel
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string KeyPrefix { get; init; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; init; }

    public DateTimeOffset? LastUsedUtc { get; init; }

    public bool IsRevoked { get; init; }
}
