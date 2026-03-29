namespace UnifiPlus.Web.Models;

public sealed class DashboardViewModel
{
    public string UserId { get; init; } = string.Empty;

    public bool IsAdmin { get; init; }

    public IReadOnlyList<WanInterface> AvailableWans { get; init; } = [];

    public IReadOnlyList<AssignedClientViewModel> AssignedClients { get; init; } = [];

    public IReadOnlyList<UniFiClient> AllClients { get; init; } = [];
}
