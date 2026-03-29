namespace UnifiPlus.Web.Models;

public sealed class AccountViewModel
{
    public string UserId { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

    public string DisplayName => string.IsNullOrWhiteSpace(UserId) ? "User" : UserId;

    public IReadOnlyList<WanInterface> AvailableWans { get; init; } = [];

    public IReadOnlyList<UniFiClient> AvailableClients { get; init; } = [];

    public IReadOnlyList<AssignedClientViewModel> AssignedClients { get; init; } = [];

    public ChangePasswordRequest PasswordForm { get; init; } = new();
}
