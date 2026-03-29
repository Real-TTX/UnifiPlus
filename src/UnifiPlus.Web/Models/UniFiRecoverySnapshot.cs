namespace UnifiPlus.Web.Models;

public sealed class UniFiRecoverySnapshot
{
    public IReadOnlyList<string> UserIds { get; init; } = [];

    public int ClaimedClientCount { get; init; }

    public int UplinkRuleCount { get; init; }

    public int BandwidthRuleCount { get; init; }
}
