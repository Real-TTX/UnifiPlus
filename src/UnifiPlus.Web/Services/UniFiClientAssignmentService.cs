using System.Security.Claims;
using UnifiPlus.Web.Models;

namespace UnifiPlus.Web.Services;

public sealed class UniFiClientAssignmentService : IUniFiClientAssignmentService
{
    private readonly IUniFiApiClient _uniFiApiClient;
    private readonly ILocalUserManagementService _localUserManagementService;

    public UniFiClientAssignmentService(IUniFiApiClient uniFiApiClient, ILocalUserManagementService localUserManagementService)
    {
        _uniFiApiClient = uniFiApiClient;
        _localUserManagementService = localUserManagementService;
    }

    public async Task<DashboardViewModel> BuildDashboardAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var clients = await _uniFiApiClient.GetClientsAsync(userId, cancellationToken);
        var aliases = await _localUserManagementService.GetClientAliasesAsync(userId, cancellationToken);
        clients = ApplyAliases(clients, aliases);
        var wans = await _uniFiApiClient.GetWanInterfacesAsync(cancellationToken);
        var assignedClients = BuildAssignedClients(clients, userId, wans);

        return new DashboardViewModel
        {
            UserId = userId,
            IsAdmin = user.IsInRole("Admin"),
            AvailableWans = wans,
            AssignedClients = assignedClients,
            AllClients = clients
        };
    }

    public async Task<AccountViewModel> BuildAccountAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        var clients = await _uniFiApiClient.GetClientsAsync(userId, cancellationToken);
        var aliases = await _localUserManagementService.GetClientAliasesAsync(userId, cancellationToken);
        clients = ApplyAliases(clients, aliases);
        var wans = await _uniFiApiClient.GetWanInterfacesAsync(cancellationToken);

        return new AccountViewModel
        {
            UserId = userId,
            Role = user.IsInRole("Admin") ? "Admin" : "User",
            AvailableWans = wans,
            AvailableClients = clients,
            AssignedClients = BuildAssignedClients(clients, userId, wans)
        };
    }

    public Task AssignClientAsync(ClaimsPrincipal user, string clientId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        return _uniFiApiClient.AssignClientToUserAsync(userId, clientId, cancellationToken);
    }

    public Task UpdateWanAsync(ClaimsPrincipal user, string clientId, int wanProfile, CancellationToken cancellationToken)
    {
        return UpdateWanAsync(user, clientId, wanProfile.ToString(), cancellationToken);
    }

    public Task UpdateWanAsync(ClaimsPrincipal user, string clientId, string wanId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        return _uniFiApiClient.UpdateWanPolicyAsync(userId, clientId, wanId, cancellationToken);
    }

    public Task UpdateBandwidthAsync(ClaimsPrincipal user, string clientId, int? downloadLimitMbps, int? uploadLimitMbps, CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        return _uniFiApiClient.UpdateBandwidthLimitAsync(userId, clientId, downloadLimitMbps, uploadLimitMbps, cancellationToken);
    }

    private static string GetUserId(ClaimsPrincipal user)
    {
        return user.Identity?.Name ?? "demo-user";
    }

    private static List<AssignedClientViewModel> BuildAssignedClients(
        IReadOnlyList<UniFiClient> clients,
        string userId,
        IReadOnlyList<WanInterface> wans)
    {
        return clients
            .Where(client => client.AssignedToCurrentUser)
            .Select((client, index) =>
            {
                var selectedWan = wans.FirstOrDefault(wan => string.Equals(wan.Id, client.SelectedWanId, StringComparison.OrdinalIgnoreCase))
                    ?? wans.FirstOrDefault(wan => wan.IsActive)
                    ?? wans.FirstOrDefault();

                return new AssignedClientViewModel
                {
                    ClientId = client.Id,
                    ClientName = client.Name,
                    Hostname = client.Hostname,
                    IpAddress = client.IpAddress,
                    MacAddress = client.MacAddress,
                    Manufacturer = client.Manufacturer,
                    ConnectionType = client.ConnectionType,
                    IsOnline = client.IsOnline,
                    LastSeenUtc = client.LastSeenUtc,
                    PolicyName = string.IsNullOrWhiteSpace(client.PolicyName) ? $"UP-{userId}-{index + 1}" : client.PolicyName,
                    AliasName = client.AliasName,
                    ActiveWanId = selectedWan?.Id ?? string.Empty,
                    ActiveWanName = selectedWan?.Name ?? "No UniFi WAN detected",
                    AvailableWans = wans,
                    BandwidthRuleId = client.BandwidthRuleId,
                    BandwidthRuleName = client.BandwidthRuleName,
                    DownloadLimitMbps = client.DownloadLimitMbps,
                    UploadLimitMbps = client.UploadLimitMbps
                };
            })
            .ToList();
    }

    private static IReadOnlyList<UniFiClient> ApplyAliases(
        IReadOnlyList<UniFiClient> clients,
        IReadOnlyDictionary<string, string> aliases)
    {
        if (aliases.Count == 0)
        {
            return clients;
        }

        return clients
            .Select(client => new UniFiClient
            {
                Id = client.Id,
                Name = client.Name,
                Hostname = client.Hostname,
                IpAddress = client.IpAddress,
                AssignedToCurrentUser = client.AssignedToCurrentUser,
                PolicyName = client.PolicyName,
                MacAddress = client.MacAddress,
                Manufacturer = client.Manufacturer,
                ConnectionType = client.ConnectionType,
                IsOnline = client.IsOnline,
                LastSeenUtc = client.LastSeenUtc,
                SelectedWanId = client.SelectedWanId,
                AliasName = aliases.TryGetValue(client.Id, out var alias) ? alias : string.Empty,
                BandwidthRuleId = client.BandwidthRuleId,
                BandwidthRuleName = client.BandwidthRuleName,
                DownloadLimitMbps = client.DownloadLimitMbps,
                UploadLimitMbps = client.UploadLimitMbps
            })
            .ToList();
    }
}
