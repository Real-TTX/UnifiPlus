namespace UnifiPlus.Web.Models;

public sealed class UserEditViewModel
{
    public ManagedUserViewModel User { get; init; } = new();

    public AdminUserRoleUpdateRequest RoleForm { get; init; } = new();

    public AdminUserPasswordResetRequest PasswordForm { get; init; } = new();

    public AdminUserDeleteRequest DeleteForm { get; init; } = new();

    public IReadOnlyList<AssignedClientViewModel> AssignedClients { get; init; } = [];

    public IReadOnlyList<UniFiClient> AvailableClients { get; init; } = [];

    public IReadOnlyList<ApiKeyListItemViewModel> ApiKeys { get; init; } = [];

    public AdminCreateApiKeyRequest ApiKeyForm { get; init; } = new();

    public string? StatusMessage { get; init; }

    public bool StatusIsSuccess { get; init; }
}
