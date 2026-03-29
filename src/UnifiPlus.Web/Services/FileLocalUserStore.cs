using System.Text.Json;
using Microsoft.Extensions.Options;
using UnifiPlus.Web.Data;
using UnifiPlus.Web.Options;

namespace UnifiPlus.Web.Services;

public sealed class FileLocalUserStore : ILocalUserStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _rootPath;
    private readonly string _storagePath;

    public FileLocalUserStore(IOptions<DataStorageOptions> options)
    {
        _rootPath = options.Value.RootPath;
        _storagePath = Path.Combine(_rootPath, "users.json");
    }

    public string StoragePath => _storagePath;

    public async Task<IReadOnlyList<LocalUser>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storagePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_storagePath);
        var users = await JsonSerializer.DeserializeAsync<List<LocalUser>>(stream, JsonOptions, cancellationToken);
        return users ?? [];
    }

    public async Task<LocalUser?> FindByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        var users = await GetAllAsync(cancellationToken);
        return users.FirstOrDefault(user => string.Equals(user.UserId, userId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken)
    {
        var users = await GetAllAsync(cancellationToken);
        return users.Count > 0;
    }

    public async Task SaveAllAsync(IReadOnlyList<LocalUser> users, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootPath);
        await using var stream = File.Create(_storagePath);
        await JsonSerializer.SerializeAsync(stream, users, JsonOptions, cancellationToken);
    }
}
