namespace UnifiPlus.Web.Models;

public sealed class AdminUserDevicesViewModel
{
    public ManagedUserViewModel User { get; init; } = new();

    public IReadOnlyList<UniFiClient> AvailableClients { get; init; } = [];

    public IReadOnlyList<AssignedClientViewModel> AssignedClients { get; init; } = [];

    public IReadOnlyList<int> DownloadTemplateValuesMbps { get; init; } = [];

    public IReadOnlyList<int> UploadTemplateValuesMbps { get; init; } = [];

    public string? StatusMessage { get; init; }

    public bool StatusIsSuccess { get; init; }
}
