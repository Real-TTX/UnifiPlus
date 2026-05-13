namespace UnifiPlus.Web.Models.Api;

public sealed class ApiHealthResponse
{
    public string Name { get; init; } = "UnifiPlus API";

    public string Status { get; init; } = "ok";

    public DateTimeOffset UtcTime { get; init; } = DateTimeOffset.UtcNow;
}
