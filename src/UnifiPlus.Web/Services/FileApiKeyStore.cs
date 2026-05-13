using System.Text.Json;
using Microsoft.Extensions.Options;
using UnifiPlus.Web.Data;
using UnifiPlus.Web.Options;

namespace UnifiPlus.Web.Services;

public sealed class FileApiKeyStore : IApiKeyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _rootPath;
    private readonly string _storagePath;

    public FileApiKeyStore(IOptions<DataStorageOptions> options)
    {
        _rootPath = options.Value.RootPath;
        _storagePath = Path.Combine(_rootPath, "api-keys.json");
    }

    public async Task<IReadOnlyList<ApiKeyRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storagePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_storagePath);
        var apiKeys = await JsonSerializer.DeserializeAsync<List<ApiKeyRecord>>(stream, JsonOptions, cancellationToken);
        return apiKeys ?? [];
    }

    public async Task SaveAllAsync(IReadOnlyList<ApiKeyRecord> apiKeys, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootPath);
        await using var stream = File.Create(_storagePath);
        await JsonSerializer.SerializeAsync(stream, apiKeys, JsonOptions, cancellationToken);
    }
}
