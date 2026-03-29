namespace UnifiPlus.Web.Models;

public sealed class UniFiRecoveryResult
{
    public UniFiRecoverySnapshot Snapshot { get; init; } = new();

    public IReadOnlyList<RecoveredUserResult> Users { get; init; } = [];
}
