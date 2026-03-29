using UnifiPlus.Web.Data;
using UnifiPlus.Web.Models;

namespace UnifiPlus.Web.Services;

public sealed class AdminSetupService : IAdminSetupService
{
    private readonly IUniFiConfigurationStore _configurationStore;
    private readonly IUniFiApiClient _uniFiApiClient;

    public AdminSetupService(IUniFiConfigurationStore configurationStore, IUniFiApiClient uniFiApiClient)
    {
        _configurationStore = configurationStore;
        _uniFiApiClient = uniFiApiClient;
    }

    public async Task<AdminSetupViewModel> BuildViewModelAsync(CancellationToken cancellationToken)
    {
        var stored = await _configurationStore.GetAsync(cancellationToken);

        return new AdminSetupViewModel
        {
            Form = stored is null ? new AdminUniFiSetupRequest() : Map(stored),
            StoragePath = _configurationStore.StoragePath,
            HasSavedConfiguration = stored is not null,
            LastUpdatedUtc = stored?.LastUpdatedUtc,
            HasStoredApiKey = !string.IsNullOrWhiteSpace(stored?.ApiKey),
            HasStoredPassword = !string.IsNullOrWhiteSpace(stored?.Password)
        };
    }

    public async Task SaveConfigurationAsync(AdminUniFiSetupRequest request, CancellationToken cancellationToken)
    {
        var existing = await _configurationStore.GetAsync(cancellationToken);
        var stored = new StoredUniFiConfiguration
        {
            BaseUrl = request.BaseUrl.Trim(),
            Site = request.Site.Trim(),
            ApiKey = string.IsNullOrWhiteSpace(request.ApiKey) ? existing?.ApiKey ?? string.Empty : request.ApiKey.Trim(),
            Username = request.Username.Trim(),
            Password = string.IsNullOrWhiteSpace(request.Password) ? existing?.Password ?? string.Empty : request.Password,
            AllowSelfSignedTls = request.AllowSelfSignedTls
        };

        await _configurationStore.SaveAsync(stored, cancellationToken);
    }

    public async Task<UniFiConnectionTestResult> TestConnectionAsync(AdminUniFiSetupRequest request, CancellationToken cancellationToken)
    {
        var effective = request;
        var existing = await _configurationStore.GetAsync(cancellationToken);
        if (existing is not null)
        {
            effective = new AdminUniFiSetupRequest
            {
                BaseUrl = request.BaseUrl,
                Site = request.Site,
                ApiKey = string.IsNullOrWhiteSpace(request.ApiKey) ? existing.ApiKey : request.ApiKey,
                Username = request.Username,
                Password = string.IsNullOrWhiteSpace(request.Password) ? existing.Password : request.Password,
                AllowSelfSignedTls = request.AllowSelfSignedTls
            };
        }

        return await _uniFiApiClient.TestConnectionAsync(effective, cancellationToken);
    }

    private static AdminUniFiSetupRequest Map(StoredUniFiConfiguration stored)
    {
        return new AdminUniFiSetupRequest
        {
            BaseUrl = stored.BaseUrl,
            Site = stored.Site,
            ApiKey = string.Empty,
            Username = stored.Username,
            Password = string.Empty,
            AllowSelfSignedTls = stored.AllowSelfSignedTls
        };
    }
}
