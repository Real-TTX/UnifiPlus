namespace UnifiPlus.Web.Options;

public sealed class UniFiOptions
{
    public const string SectionName = "UniFi";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Site { get; set; } = "default";

    public bool AllowSelfSignedTls { get; set; }
}
