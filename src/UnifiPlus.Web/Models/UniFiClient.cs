namespace UnifiPlus.Web.Models;

public sealed class UniFiClient
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Hostname { get; init; } = string.Empty;

    public string? IpAddress { get; init; }

    public bool AssignedToCurrentUser { get; init; }

    public string PolicyName { get; init; } = string.Empty;

    public string MacAddress { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public string ConnectionType { get; init; } = string.Empty;

    public bool IsOnline { get; init; }

    public DateTimeOffset? LastSeenUtc { get; init; }

    public string SelectedWanId { get; init; } = string.Empty;

    public string AliasName { get; init; } = string.Empty;

    public string BandwidthRuleId { get; init; } = string.Empty;

    public string BandwidthRuleName { get; init; } = string.Empty;

    public int? DownloadLimitMbps { get; init; }

    public int? UploadLimitMbps { get; init; }
}
