namespace UnifiPlus.Web.Models;

public sealed class BandwidthPageViewModel
{
    public string UserId { get; init; } = string.Empty;

    public bool IsAdmin { get; init; }

    public IReadOnlyList<AssignedClientViewModel> AssignedClients { get; init; } = [];
}
