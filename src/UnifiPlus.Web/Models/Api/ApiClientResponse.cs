namespace UnifiPlus.Web.Models.Api;

public sealed class ApiClientResponse
{
    public string ClientId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Hostname { get; init; } = string.Empty;

    public string? IpAddress { get; init; }

    public string MacAddress { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public string ConnectionType { get; init; } = string.Empty;

    public bool IsOnline { get; init; }

    public string PolicyName { get; init; } = string.Empty;

    public string ActiveWanId { get; init; } = string.Empty;

    public string ActiveWanName { get; init; } = string.Empty;

    public int? DownloadLimitMbps { get; init; }

    public int? UploadLimitMbps { get; init; }
}
