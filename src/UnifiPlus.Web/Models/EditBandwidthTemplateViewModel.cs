namespace UnifiPlus.Web.Models;

public sealed class EditBandwidthTemplateViewModel
{
    public BandwidthTemplateSettingsRequest Form { get; init; } = new();

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<int> CurrentTemplatesMbps { get; init; } = [];

    public string StoragePath { get; init; } = string.Empty;

    public DateTimeOffset? LastUpdatedUtc { get; init; }
}
