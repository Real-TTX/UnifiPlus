namespace UnifiPlus.Web.Models;

public sealed class AssignedClientViewModel
{
    public string ClientId { get; init; } = string.Empty;

    public string ClientName { get; init; } = string.Empty;

    public string Hostname { get; init; } = string.Empty;

    public string? IpAddress { get; init; }

    public string MacAddress { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public string ConnectionType { get; init; } = string.Empty;

    public bool IsOnline { get; init; }

    public DateTimeOffset? LastSeenUtc { get; init; }

    public string PolicyName { get; init; } = string.Empty;

    public string AliasName { get; init; } = string.Empty;

    public string ActiveWanId { get; init; } = string.Empty;

    public string ActiveWanName { get; init; } = string.Empty;

    public IReadOnlyList<WanInterface> AvailableWans { get; init; } = [];

    public string BandwidthRuleId { get; init; } = string.Empty;

    public string BandwidthRuleName { get; init; } = string.Empty;

    public int? DownloadLimitMbps { get; init; }

    public int? UploadLimitMbps { get; init; }
}
