namespace UnifiPlus.Web.Data;

public sealed class UniFiApiSnapshot
{
    public string EndpointUsed { get; init; } = string.Empty;

    public bool Authenticated { get; init; }

    public IReadOnlyList<string> Sites { get; init; } = [];

    public int ClientCount { get; init; }

    public int WanCount { get; init; }
}
