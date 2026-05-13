using System.Security.Cryptography;
using System.Text;
using UnifiPlus.Web.Data;

namespace UnifiPlus.Web.Services;

public sealed class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyStore _apiKeyStore;
    private readonly ILocalUserStore _localUserStore;

    public ApiKeyService(IApiKeyStore apiKeyStore, ILocalUserStore localUserStore)
    {
        _apiKeyStore = apiKeyStore;
        _localUserStore = localUserStore;
    }

    public async Task<(ApiKeyRecord Record, string PlaintextKey)> CreateAsync(string userId, string name, CancellationToken cancellationToken)
    {
        var normalizedUserId = userId.Trim();
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var plaintextKey = $"upk_{Convert.ToBase64String(rawBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=')}";
        var record = new ApiKeyRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = normalizedUserId,
            Name = name.Trim(),
            KeyPrefix = plaintextKey[..Math.Min(12, plaintextKey.Length)],
            KeyHash = ComputeHash(plaintextKey),
            CreatedUtc = DateTimeOffset.UtcNow
        };

        var allKeys = (await _apiKeyStore.GetAllAsync(cancellationToken)).ToList();
        allKeys.Add(record);
        await _apiKeyStore.SaveAllAsync(allKeys, cancellationToken);
        return (record, plaintextKey);
    }

    public async Task<IReadOnlyList<ApiKeyRecord>> GetForUserAsync(string userId, CancellationToken cancellationToken)
    {
        var allKeys = await _apiKeyStore.GetAllAsync(cancellationToken);
        return allKeys
            .Where(key => string.Equals(key.UserId, userId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(key => key.CreatedUtc)
            .ToList();
    }

    public async Task<bool> RevokeAsync(string userId, string keyId, CancellationToken cancellationToken)
    {
        var allKeys = (await _apiKeyStore.GetAllAsync(cancellationToken)).ToList();
        var key = allKeys.FirstOrDefault(item =>
            string.Equals(item.Id, keyId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.UserId, userId, StringComparison.OrdinalIgnoreCase));

        if (key is null || key.IsRevoked)
        {
            return false;
        }

        key.RevokedUtc = DateTimeOffset.UtcNow;
        await _apiKeyStore.SaveAllAsync(allKeys, cancellationToken);
        return true;
    }

    public async Task<LocalUser?> ValidateAsync(string rawKey, CancellationToken cancellationToken)
    {
        var keyHash = ComputeHash(rawKey.Trim());
        var allKeys = (await _apiKeyStore.GetAllAsync(cancellationToken)).ToList();
        var record = allKeys.FirstOrDefault(item =>
            !item.IsRevoked &&
            string.Equals(item.KeyHash, keyHash, StringComparison.Ordinal));

        if (record is null)
        {
            return null;
        }

        record.LastUsedUtc = DateTimeOffset.UtcNow;
        await _apiKeyStore.SaveAllAsync(allKeys, cancellationToken);
        return await _localUserStore.FindByUserIdAsync(record.UserId, cancellationToken);
    }

    private static string ComputeHash(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes);
    }
}
