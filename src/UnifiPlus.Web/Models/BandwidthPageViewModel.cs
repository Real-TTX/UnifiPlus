namespace UnifiPlus.Web.Models;

public sealed class BandwidthPageViewModel
{
    public string UserId { get; init; } = string.Empty;

    public bool IsAdmin { get; init; }

    public IReadOnlyList<AssignedClientViewModel> AssignedClients { get; init; } = [];

    public IReadOnlyList<int> DownloadTemplateValuesMbps { get; init; } = [50, 100, 200];

    public IReadOnlyList<int> UploadTemplateValuesMbps { get; init; } = [5, 10, 20];
}
