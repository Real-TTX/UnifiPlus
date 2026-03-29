namespace UnifiPlus.Web.Models;

public sealed class UserManagementViewModel
{
    public IReadOnlyList<ManagedUserViewModel> Users { get; init; } = [];

    public string CurrentUserId { get; init; } = string.Empty;

    public string? StatusMessage { get; init; }

    public bool StatusIsSuccess { get; init; }
}
