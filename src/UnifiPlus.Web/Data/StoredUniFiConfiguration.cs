namespace UnifiPlus.Web.Data;

public sealed class StoredUniFiConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;

    public string Site { get; set; } = "default";

    public string ApiKey { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool AllowSelfSignedTls { get; set; }

    public DateTimeOffset? LastUpdatedUtc { get; set; }
}
