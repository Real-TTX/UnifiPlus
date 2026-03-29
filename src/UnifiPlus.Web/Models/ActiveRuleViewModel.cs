namespace UnifiPlus.Web.Models;

public sealed class ActiveRuleViewModel
{
    public string Id { get; init; } = string.Empty;

    public string Type { get; init; } = "Uplink-Switch";

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Configuration { get; init; } = string.Empty;

    public int TargetDeviceCount { get; init; }
}
