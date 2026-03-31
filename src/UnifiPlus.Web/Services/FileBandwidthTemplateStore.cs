using System.Text.Json;
using Microsoft.Extensions.Options;
using UnifiPlus.Web.Models;
using UnifiPlus.Web.Options;

namespace UnifiPlus.Web.Services;

public sealed class FileBandwidthTemplateStore : IBandwidthTemplateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _rootPath;
    private readonly string _storagePath;

    public FileBandwidthTemplateStore(IOptions<DataStorageOptions> options)
    {
        _rootPath = options.Value.RootPath;
        _storagePath = Path.Combine(_rootPath, "bandwidth-templates.json");
    }

    public string StoragePath => _storagePath;

    public async Task<BandwidthTemplateSettings> GetAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storagePath))
        {
            return new BandwidthTemplateSettings();
        }

        await using var stream = File.OpenRead(_storagePath);
        var settings = await JsonSerializer.DeserializeAsync<BandwidthTemplateSettings>(stream, JsonOptions, cancellationToken);
        return settings ?? new BandwidthTemplateSettings();
    }

    public async Task SaveAsync(BandwidthTemplateSettings settings, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootPath);
        settings.LastUpdatedUtc = DateTimeOffset.UtcNow;

        await using var stream = File.Create(_storagePath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }
}
