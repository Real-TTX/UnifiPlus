namespace UnifiPlus.Web.Models;

public sealed class AccountDevicesViewModel
{
    public string UserId { get; init; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(UserId) ? "User" : UserId;

    public IReadOnlyList<WanInterface> AvailableWans { get; init; } = [];

    public IReadOnlyList<UniFiClient> AvailableClients { get; init; } = [];

    public IReadOnlyList<AssignedClientViewModel> AssignedClients { get; init; } = [];

    public IReadOnlyList<int> DownloadTemplateValuesMbps { get; init; } = [];

    public IReadOnlyList<int> UploadTemplateValuesMbps { get; init; } = [];
}
