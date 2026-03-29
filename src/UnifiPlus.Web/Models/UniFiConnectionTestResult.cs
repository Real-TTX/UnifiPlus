namespace UnifiPlus.Web.Models;

public sealed class UniFiConnectionTestResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public int? StatusCode { get; init; }

    public string EndpointUsed { get; init; } = string.Empty;

    public IReadOnlyList<string> Sites { get; init; } = [];

    public IReadOnlyList<string> WanNames { get; init; } = [];

    public int ClientCount { get; init; }

    public int WanCount { get; init; }
}
