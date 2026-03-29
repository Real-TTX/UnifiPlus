using System.Text.Json;
using Microsoft.Extensions.Options;
using UnifiPlus.Web.Data;
using UnifiPlus.Web.Options;

namespace UnifiPlus.Web.Services;

public sealed class FileUniFiConfigurationStore : IUniFiConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _rootPath;
    private readonly string _storagePath;

    public FileUniFiConfigurationStore(IOptions<DataStorageOptions> options)
    {
        _rootPath = options.Value.RootPath;
        _storagePath = Path.Combine(_rootPath, "unifi-settings.json");
    }

    public string StoragePath => _storagePath;

    public async Task<StoredUniFiConfiguration?> GetAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storagePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(_storagePath);
        return await JsonSerializer.DeserializeAsync<StoredUniFiConfiguration>(stream, JsonOptions, cancellationToken);
    }

    public async Task SaveAsync(StoredUniFiConfiguration configuration, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootPath);
        configuration.LastUpdatedUtc = DateTimeOffset.UtcNow;

        await using var stream = File.Create(_storagePath);
        await JsonSerializer.SerializeAsync(stream, configuration, JsonOptions, cancellationToken);
    }
}
