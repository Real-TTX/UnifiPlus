namespace UnifiPlus.Web.Models;

public sealed class WanInterface
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
