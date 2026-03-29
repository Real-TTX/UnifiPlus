using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UnifiPlus.Web.Data;
using UnifiPlus.Web.Models;
using UnifiPlus.Web.Options;

namespace UnifiPlus.Web.Services;

public sealed class UniFiApiClient : IUniFiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly UniFiOptions _options;
    private readonly IUniFiConfigurationStore _configurationStore;
    private readonly ILogger<UniFiApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public UniFiApiClient(
        HttpClient httpClient,
        IOptions<UniFiOptions> options,
        IUniFiConfigurationStore configurationStore,
        ILogger<UniFiApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _configurationStore = configurationStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UniFiClient>> GetClientsAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await ResolveConfigurationAsync(cancellationToken);
            using var session = await CreateSessionAsync(configuration, cancellationToken);

            if (string.IsNullOrWhiteSpace(session.Site))
            {
                return [];
            }

            var rawClients = await GetClientsRawAsync(session.Client, session.Site, cancellationToken);
            var metadata = await GetClientMetadataAsync(session.Client, session.Site, cancellationToken);
            var wanNetworks = await GetWanNetworkMapAsync(session.Client, session.Site, cancellationToken);
            var qosRules = await GetQosRuleMapAsync(session.Client, session.Site, wanNetworks, cancellationToken);
            var trafficRoutes = await GetTrafficRoutesAsync(session.Client, session.Site, wanNetworks, cancellationToken);
            return rawClients
                .Select((item, index) => MapClient(item, metadata, trafficRoutes, qosRules, userId, index))
                .Where(client => !string.IsNullOrWhiteSpace(client.Id))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load UniFi clients for user {UserId}.", userId);
            return [];
        }
    }

    public async Task<IReadOnlyList<WanInterface>> GetWanInterfacesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await ResolveConfigurationAsync(cancellationToken);
            using var session = await CreateSessionAsync(configuration, cancellationToken);

            if (string.IsNullOrWhiteSpace(session.Site))
            {
                return [];
            }

            var rawWans = await GetWansRawAsync(session.Client, session.Site, cancellationToken);
            var wanNetworks = await GetWanNetworkMapAsync(session.Client, session.Site, cancellationToken);
            return BuildWanInterfaces(rawWans, wanNetworks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load UniFi WAN interfaces.");
            return [];
        }
    }

    public async Task<UniFiConnectionTestResult> TestConnectionAsync(AdminUniFiSetupRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.BaseUrl, UriKind.Absolute, out _))
        {
            return new UniFiConnectionTestResult
            {
                Success = false,
                Message = "The UniFi base URL is not a valid absolute URL."
            };
        }

        try
        {
            var configuration = new StoredUniFiConfiguration
            {
                BaseUrl = request.BaseUrl.Trim(),
                Site = request.Site.Trim(),
                ApiKey = request.ApiKey.Trim(),
                Username = request.Username.Trim(),
                Password = request.Password,
                AllowSelfSignedTls = request.AllowSelfSignedTls
            };

            using var session = await CreateSessionAsync(configuration, cancellationToken);
            var sites = await GetSitesAsync(session.Client, cancellationToken);
            var effectiveSite = ResolveSite(configuration.Site, sites);
            var clients = string.IsNullOrWhiteSpace(effectiveSite)
                ? []
                : await GetClientsRawAsync(session.Client, effectiveSite, cancellationToken);
            var wans = string.IsNullOrWhiteSpace(effectiveSite)
                ? []
                : await GetWansRawAsync(session.Client, effectiveSite, cancellationToken);
            var wanNetworks = string.IsNullOrWhiteSpace(effectiveSite)
                ? new Dictionary<string, WanMapping>(StringComparer.OrdinalIgnoreCase)
                : await GetWanNetworkMapAsync(session.Client, effectiveSite, cancellationToken);
            var resolvedWans = BuildWanInterfaces(wans, wanNetworks);
            var wanNames = resolvedWans
                .Where(wan => !string.IsNullOrWhiteSpace(wan.Name))
                .Select(wan => wan.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new UniFiConnectionTestResult
            {
                Success = true,
                StatusCode = 200,
                EndpointUsed = session.EndpointRoot,
                Sites = sites,
                WanNames = wanNames,
                ClientCount = clients.Count,
                WanCount = resolvedWans.Count,
                Message = $"UniFi API reachable. Discovered {sites.Count} site(s), {clients.Count} client(s) and {resolvedWans.Count} uplink candidate(s)."
            };
        }
        catch (HttpRequestException ex)
        {
            return new UniFiConnectionTestResult
            {
                Success = false,
                Message = $"Connection test failed: {ex.Message}"
            };
        }
        catch (InvalidOperationException ex)
        {
            return new UniFiConnectionTestResult
            {
                Success = false,
                Message = ex.Message
            };
        }
        catch (Exception ex)
        {
            return new UniFiConnectionTestResult
            {
                Success = false,
                Message = $"Connection test failed: {ex.Message}"
            };
        }
    }

    public async Task AssignClientToUserAsync(string userId, string clientId, CancellationToken cancellationToken)
    {
        var configuration = await ResolveConfigurationAsync(cancellationToken);
        using var session = await CreateSessionAsync(configuration, cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Site))
        {
            throw new InvalidOperationException("No UniFi site is configured.");
        }

        var metadata = await GetClientMetadataAsync(session.Client, session.Site, cancellationToken);
        var nextIndex = metadata.Values
            .Select(item => item.Note)
            .Select(note => TryParseClaimIndex(note, userId))
            .Where(index => index > 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var note = $"UP-{userId}-{nextIndex}";
        using var content = new StringContent(
            JsonSerializer.Serialize(new { note }),
            Encoding.UTF8,
            "application/json");
        using var response = await session.Client.PutAsync($"/proxy/network/api/s/{session.Site}/rest/user/{clientId}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateWanPolicyAsync(string userId, string clientId, string wanId, CancellationToken cancellationToken)
    {
        var configuration = await ResolveConfigurationAsync(cancellationToken);
        using var session = await CreateSessionAsync(configuration, cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Site))
        {
            throw new InvalidOperationException("No UniFi site is configured.");
        }

        var metadata = await GetClientMetadataAsync(session.Client, session.Site, cancellationToken);
        var wanNetworks = await GetWanNetworkMapAsync(session.Client, session.Site, cancellationToken);
        var trafficRoutes = await GetTrafficRoutesAsync(session.Client, session.Site, wanNetworks, cancellationToken);
        var qosRules = await GetQosRuleMapAsync(session.Client, session.Site, wanNetworks, cancellationToken);

        var clientMetadata = GetClientMetadata(metadata, clientId, string.Empty);
        if (clientMetadata is null || string.IsNullOrWhiteSpace(clientMetadata.MacAddress))
        {
            throw new InvalidOperationException("The selected UniFi client could not be resolved.");
        }

        var policyName = clientMetadata.Note;
        if (string.IsNullOrWhiteSpace(policyName) || !policyName.StartsWith($"UP-{userId}-", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Claim this device first before switching its WAN.");
        }

        if (!wanNetworks.TryGetValue(wanId, out var network))
        {
            throw new InvalidOperationException("The selected WAN could not be resolved in UniFi.");
        }

        var wanDisplayName = ResolveWanDisplayName(wanId, wanNetworks);
        var routeName = $"{policyName} -> {wanDisplayName}";
        var routeDescription = $"UnifiPlus route for {policyName} ({clientMetadata.DisplayName}) to {wanDisplayName}";

        var existingRoute = trafficRoutes.Values.FirstOrDefault(route =>
            string.Equals(route.PolicyName, policyName, StringComparison.OrdinalIgnoreCase) ||
            route.TargetDevices.Any(device => string.Equals(device.ClientMac, clientMetadata.MacAddress, StringComparison.OrdinalIgnoreCase)));

        var payload = JsonSerializer.Serialize(new
        {
            name = routeName,
            description = routeDescription,
            enabled = true,
            matching_target = "INTERNET",
            network_id = network.Id,
            target_devices = new[]
            {
                new
                {
                    type = "CLIENT",
                    client_mac = clientMetadata.MacAddress
                }
            }
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = existingRoute is null
            ? await session.Client.PostAsync($"/proxy/network/v2/api/site/{session.Site}/trafficroutes", content, cancellationToken)
            : await session.Client.PutAsync($"/proxy/network/v2/api/site/{session.Site}/trafficroutes/{existingRoute.Id}", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var qosRule = qosRules.Values.FirstOrDefault(rule =>
            IsUnifiPlusQosRule(rule) &&
            rule.TargetClientMacs.Any(mac => string.Equals(mac, clientMetadata.MacAddress, StringComparison.OrdinalIgnoreCase)));
        if (qosRule is not null)
        {
            var qosPayload = JsonSerializer.Serialize(new
            {
                _id = qosRule.Id,
                enabled = qosRule.Enabled,
                name = qosRule.Name,
                wan_or_vpn_network = network.Id,
                index = qosRule.Index,
                source = new
                {
                    matching_target = "CLIENT",
                    client_macs = new[] { clientMetadata.MacAddress },
                    port_matching_type = "ANY"
                },
                destination = new
                {
                    matching_target = "ANY",
                    port_matching_type = "ANY"
                },
                schedule = new
                {
                    mode = "ALWAYS"
                },
                objective = "LIMIT",
                download_limit_kbps = qosRule.DownloadLimitKbps,
                download_burst = "OFF",
                upload_limit_kbps = qosRule.UploadLimitKbps,
                upload_burst = "OFF"
            });

            using var qosContent = new StringContent(qosPayload, Encoding.UTF8, "application/json");
            using var qosResponse = await session.Client.PutAsync($"/proxy/network/v2/api/site/{session.Site}/qos-rules/{qosRule.Id}", qosContent, cancellationToken);
            qosResponse.EnsureSuccessStatusCode();
        }
    }

    public async Task UpdateBandwidthLimitAsync(string userId, string clientId, int? downloadLimitMbps, int? uploadLimitMbps, CancellationToken cancellationToken)
    {
        if (!downloadLimitMbps.HasValue && !uploadLimitMbps.HasValue)
        {
            throw new InvalidOperationException("Enter at least one upload or download limit.");
        }

        var configuration = await ResolveConfigurationAsync(cancellationToken);
        using var session = await CreateSessionAsync(configuration, cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Site))
        {
            throw new InvalidOperationException("No UniFi site is configured.");
        }

        var metadata = await GetClientMetadataAsync(session.Client, session.Site, cancellationToken);
        var clientMetadata = GetClientMetadata(metadata, clientId, string.Empty);
        if (clientMetadata is null)
        {
            throw new InvalidOperationException("The selected UniFi client could not be resolved.");
        }

        var policyName = clientMetadata.Note;
        if (string.IsNullOrWhiteSpace(policyName) || !policyName.StartsWith($"UP-{userId}-", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Claim this device first before applying a bandwidth limit.");
        }

        var wanNetworks = await GetWanNetworkMapAsync(session.Client, session.Site, cancellationToken);
        var trafficRoutes = await GetTrafficRoutesAsync(session.Client, session.Site, wanNetworks, cancellationToken);
        var qosRules = await GetQosRuleMapAsync(session.Client, session.Site, wanNetworks, cancellationToken);
        var ruleName = BuildBandwidthRuleName(policyName);
        var existingRule = qosRules.Values.FirstOrDefault(rule =>
            string.Equals(rule.Name, ruleName, StringComparison.OrdinalIgnoreCase) ||
            rule.TargetClientMacs.Any(mac => string.Equals(mac, clientMetadata.MacAddress, StringComparison.OrdinalIgnoreCase)));

        var targetWanNetworkId = ResolveBandwidthWanNetworkId(clientMetadata.MacAddress, trafficRoutes, wanNetworks)
            ?? wanNetworks.Values.FirstOrDefault().Id;
        if (string.IsNullOrWhiteSpace(targetWanNetworkId))
        {
            throw new InvalidOperationException("No UniFi uplink could be resolved for the bandwidth policy.");
        }

        var payload = JsonSerializer.Serialize(new
        {
            _id = existingRule?.Id,
            enabled = true,
            name = ruleName,
            wan_or_vpn_network = targetWanNetworkId,
            index = existingRule?.Index ?? 10000 + TryParseClaimIndex(policyName, userId),
            source = new
            {
                matching_target = "CLIENT",
                client_macs = new[] { clientMetadata.MacAddress },
                port_matching_type = "ANY"
            },
            destination = new
            {
                matching_target = "ANY",
                port_matching_type = "ANY"
            },
            schedule = new
            {
                mode = "ALWAYS"
            },
            objective = "LIMIT",
            download_limit_kbps = downloadLimitMbps.HasValue ? downloadLimitMbps.Value * 1000 : 0,
            download_burst = "OFF",
            upload_limit_kbps = uploadLimitMbps.HasValue ? uploadLimitMbps.Value * 1000 : 0,
            upload_burst = "OFF"
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = existingRule is null
            ? await session.Client.PostAsync($"/proxy/network/v2/api/site/{session.Site}/qos-rules", content, cancellationToken)
            : await session.Client.PutAsync($"/proxy/network/v2/api/site/{session.Site}/qos-rules/{existingRule.Id}", content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<ActiveRuleViewModel>> GetActiveRulesAsync(CancellationToken cancellationToken)
    {
        var configuration = await ResolveConfigurationAsync(cancellationToken);
        using var session = await CreateSessionAsync(configuration, cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Site))
        {
            return [];
        }

        var wanNetworks = await GetWanNetworkMapAsync(session.Client, session.Site, cancellationToken);
        var qosRules = await GetQosRuleMapAsync(session.Client, session.Site, wanNetworks, cancellationToken);
        var routes = await GetTrafficRouteListAsync(session.Client, session.Site, wanNetworks, cancellationToken);
        var uplinkRules = routes
            .Where(route => IsUnifiPlusRoute(route))
            .Select(route => new ActiveRuleViewModel
            {
                Id = $"uplink:{route.Id}",
                Type = "Uplink-Switch",
                Name = string.IsNullOrWhiteSpace(route.Name) ? route.PolicyName : route.Name,
                Description = route.Description,
                Configuration = ResolveWanDisplayName(route.NetworkGroup, wanNetworks),
                TargetDeviceCount = route.TargetDevices.Count
            })
            .ToList();

        var bandwidthRules = qosRules.Values
            .Where(IsUnifiPlusQosRule)
            .Select(item => new ActiveRuleViewModel
            {
                Id = $"bandwidth:{item.Id}",
                Type = "Bandwidth-Limiter",
                Name = item.Name,
                Description = $"Visible UniFi QoS rule managed by UnifiPlus",
                Configuration = FormatBandwidthConfiguration(item, wanNetworks),
                TargetDeviceCount = item.TargetClientMacs.Count
            })
            .ToList();

        return uplinkRules
            .Concat(bandwidthRules)
            .OrderBy(rule => rule.Type)
            .ThenBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task DeleteActiveRuleAsync(string ruleId, CancellationToken cancellationToken)
    {
        var separatorIndex = ruleId.IndexOf(':');
        var ruleType = separatorIndex > 0 ? ruleId[..separatorIndex] : string.Empty;
        var nativeId = separatorIndex > 0 ? ruleId[(separatorIndex + 1)..] : ruleId;

        var configuration = await ResolveConfigurationAsync(cancellationToken);
        using var session = await CreateSessionAsync(configuration, cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Site))
        {
            throw new InvalidOperationException("No UniFi site is configured.");
        }

        if (string.Equals(ruleType, "uplink", StringComparison.OrdinalIgnoreCase))
        {
            var wanNetworks = await GetWanNetworkMapAsync(session.Client, session.Site, cancellationToken);
            var routes = await GetTrafficRouteListAsync(session.Client, session.Site, wanNetworks, cancellationToken);
            var route = routes.FirstOrDefault(item => string.Equals(item.Id, nativeId, StringComparison.OrdinalIgnoreCase));
            if (route is null || !IsUnifiPlusRoute(route))
            {
                throw new InvalidOperationException("Only UnifiPlus-created rules can be deleted here.");
            }

            using var deleteRouteResponse = await session.Client.DeleteAsync($"/proxy/network/v2/api/site/{session.Site}/trafficroutes/{nativeId}", cancellationToken);
            deleteRouteResponse.EnsureSuccessStatusCode();
            return;
        }

        if (string.Equals(ruleType, "bandwidth", StringComparison.OrdinalIgnoreCase))
        {
            var qosRules = await GetQosRuleMapAsync(session.Client, session.Site, await GetWanNetworkMapAsync(session.Client, session.Site, cancellationToken), cancellationToken);
            if (!qosRules.TryGetValue(nativeId, out var qosRule) || !IsUnifiPlusQosRule(qosRule))
            {
                throw new InvalidOperationException("Only UnifiPlus-created rules can be deleted here.");
            }

            using var deleteRuleResponse = await session.Client.DeleteAsync($"/proxy/network/v2/api/site/{session.Site}/qos-rules/{nativeId}", cancellationToken);
            deleteRuleResponse.EnsureSuccessStatusCode();
            return;
        }

        throw new InvalidOperationException("Unknown rule type.");
    }

    public async Task<UniFiRecoverySnapshot> GetRecoverySnapshotAsync(CancellationToken cancellationToken)
    {
        var configuration = await ResolveConfigurationAsync(cancellationToken);
        using var session = await CreateSessionAsync(configuration, cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Site))
        {
            return new UniFiRecoverySnapshot();
        }

        var metadata = await GetClientMetadataAsync(session.Client, session.Site, cancellationToken);
        var wanNetworks = await GetWanNetworkMapAsync(session.Client, session.Site, cancellationToken);
        var trafficRoutes = await GetTrafficRouteListAsync(session.Client, session.Site, wanNetworks, cancellationToken);
        var qosRules = await GetQosRuleMapAsync(session.Client, session.Site, wanNetworks, cancellationToken);

        var recoveredUsers = metadata.Values
            .Select(item => item.Note)
            .Where(note => !string.IsNullOrWhiteSpace(note))
            .Select(TryExtractUserIdFromPolicy)
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(userId => userId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var claimedClientCount = metadata.Values
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Count(item => !string.IsNullOrWhiteSpace(TryExtractUserIdFromPolicy(item.Note)));

        return new UniFiRecoverySnapshot
        {
            UserIds = recoveredUsers,
            ClaimedClientCount = claimedClientCount,
            UplinkRuleCount = trafficRoutes.Count(IsUnifiPlusRoute),
            BandwidthRuleCount = qosRules.Values.Count(IsUnifiPlusQosRule)
        };
    }

    private async Task<StoredUniFiConfiguration> ResolveConfigurationAsync(CancellationToken cancellationToken)
    {
        var stored = await _configurationStore.GetAsync(cancellationToken);
        return stored ?? new StoredUniFiConfiguration
        {
            BaseUrl = _options.BaseUrl,
            Site = _options.Site,
            ApiKey = _options.ApiKey,
            Username = _options.Username,
            Password = _options.Password,
            AllowSelfSignedTls = _options.AllowSelfSignedTls
        };
    }

    private async Task<UniFiSession> CreateSessionAsync(StoredUniFiConfiguration configuration, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(configuration.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("No valid UniFi base URL is configured.");
        }

        var cookies = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookies
        };

        if (configuration.AllowSelfSignedTls)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var client = new HttpClient(handler)
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(15)
        };

        var usingApiKey = !string.IsNullOrWhiteSpace(configuration.ApiKey);
        if (usingApiKey)
        {
            client.DefaultRequestHeaders.Add("X-API-Key", configuration.ApiKey);
        }
        else if (!string.IsNullOrWhiteSpace(configuration.Username) && !string.IsNullOrWhiteSpace(configuration.Password))
        {
            await LoginWithCredentialsAsync(client, configuration, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Configure either an API key or a username and password for UniFi.");
        }

        return new UniFiSession
        {
            Client = client,
            Site = configuration.Site,
            EndpointRoot = baseUri.ToString().TrimEnd('/'),
            Authenticated = usingApiKey || cookies.Count > 0
        };
    }

    private static async Task LoginWithCredentialsAsync(HttpClient client, StoredUniFiConfiguration configuration, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            username = configuration.Username,
            password = configuration.Password
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/auth/login", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"UniFi login failed with HTTP {(int)response.StatusCode}.");
        }
    }

    private static async Task<List<string>> GetSitesAsync(HttpClient client, CancellationToken cancellationToken)
    {
        using var document = await GetJsonAsync(client, "/v1/sites", cancellationToken)
            ?? await GetJsonAsync(client, "/proxy/network/integration/v1/sites", cancellationToken);

        if (document is null)
        {
            return [];
        }

        return ExtractArray(document.RootElement)
            .Select(item => ReadString(item, "internalReference", "siteId", "name", "id", "_id"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<List<JsonElement>> GetClientsRawAsync(HttpClient client, string site, CancellationToken cancellationToken)
    {
        var candidates = new[]
        {
            await GetJsonAsync(client, $"/v1/sites/{site}/clients", cancellationToken),
            await GetJsonAsync(client, $"/proxy/network/integration/v1/sites/{site}/clients", cancellationToken),
            await GetJsonAsync(client, $"/proxy/network/api/s/{site}/stat/sta", cancellationToken)
        };

        List<JsonElement>? firstParsed = null;
        foreach (var document in candidates)
        {
            var items = ExtractElements(document);
            if (items.Count > 0)
            {
                DisposeAll(candidates);
                return items;
            }

            firstParsed ??= items;
        }

        DisposeAll(candidates);
        return firstParsed ?? [];
    }

    private static async Task<List<JsonElement>> GetWansRawAsync(HttpClient client, string site, CancellationToken cancellationToken)
    {
        var candidates = new[]
        {
            await GetJsonAsync(client, $"/v1/sites/{site}/wan-interfaces", cancellationToken),
            await GetJsonAsync(client, $"/v1/sites/{site}/internet", cancellationToken),
            await GetJsonAsync(client, $"/proxy/network/integration/v1/sites/{site}/wan-interfaces", cancellationToken),
            await GetJsonAsync(client, $"/proxy/network/integration/v1/sites/{site}/internet", cancellationToken)
        };

        List<JsonElement>? firstParsed = null;
        foreach (var document in candidates)
        {
            var items = ExtractElements(document);
            if (items.Count > 0)
            {
                DisposeAll(candidates);
                return items;
            }

            firstParsed ??= items;
        }

        DisposeAll(candidates);
        if (firstParsed is { Count: > 0 })
        {
            return firstParsed;
        }

        return await GetLegacyWansRawAsync(client, site, cancellationToken);
    }

    private static async Task<Dictionary<string, WanMapping>> GetWanNetworkMapAsync(
        HttpClient client,
        string site,
        CancellationToken cancellationToken)
    {
        using var document = await GetJsonAsync(client, $"/proxy/network/api/s/{site}/rest/networkconf", cancellationToken);
        var result = new Dictionary<string, WanMapping>(StringComparer.OrdinalIgnoreCase);
        if (document is null)
        {
            return result;
        }

        foreach (var item in ExtractArray(document.RootElement))
        {
            var purpose = ReadString(item, "purpose");
            var isWan = string.Equals(purpose, "wan", StringComparison.OrdinalIgnoreCase);
            var isVpnClient = string.Equals(purpose, "vpn-client", StringComparison.OrdinalIgnoreCase);
            if (!isWan && !isVpnClient)
            {
                continue;
            }

            var networkGroup = ReadString(item, "wan_networkgroup", "attr_hidden_id");
            var id = ReadString(item, "_id", "id");
            var name = ReadString(item, "name");
            var key = !string.IsNullOrWhiteSpace(networkGroup)
                ? networkGroup
                : isVpnClient && !string.IsNullOrWhiteSpace(id)
                    ? $"VPNCLIENT:{id}"
                    : string.Empty;
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(id))
            {
                result[key] = new WanMapping
                {
                    Id = id,
                    Name = FirstNonEmpty(name, key),
                    Purpose = purpose
                };
            }
        }

        return result;
    }

    private static List<WanInterface> BuildWanInterfaces(
        IReadOnlyList<JsonElement> rawWans,
        IReadOnlyDictionary<string, WanMapping> wanNetworks)
    {
        var results = new List<WanInterface>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in rawWans)
        {
            var mapped = MapWan(item, wanNetworks);
            if (string.IsNullOrWhiteSpace(mapped.Id) || !seen.Add(mapped.Id))
            {
                continue;
            }

            results.Add(mapped);
        }

        foreach (var pair in wanNetworks)
        {
            if (!seen.Add(pair.Key))
            {
                continue;
            }

            results.Add(new WanInterface
            {
                Id = pair.Key,
                Name = pair.Value.Name,
                IsActive = false
            });
        }

        return results
            .OrderByDescending(wan => wan.IsActive)
            .ThenBy(wan => wan.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<Dictionary<string, TrafficRouteInfo>> GetTrafficRoutesAsync(
        HttpClient client,
        string site,
        IReadOnlyDictionary<string, WanMapping> wanNetworks,
        CancellationToken cancellationToken)
    {
        return (await GetTrafficRouteListAsync(client, site, wanNetworks, cancellationToken))
            .SelectMany(route => route.TargetDevices.Select(device => new KeyValuePair<string, TrafficRouteInfo>(device.ClientMac, route)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<List<TrafficRouteInfo>> GetTrafficRouteListAsync(
        HttpClient client,
        string site,
        IReadOnlyDictionary<string, WanMapping> wanNetworks,
        CancellationToken cancellationToken)
    {
        using var document = await GetJsonAsync(client, $"/proxy/network/v2/api/site/{site}/trafficroutes", cancellationToken);
        var routes = new List<TrafficRouteInfo>();
        if (document is null || document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return routes;
        }

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var route = new TrafficRouteInfo
            {
                Id = ReadString(item, "_id", "id"),
                Name = ReadString(item, "name"),
                Description = ReadString(item, "description"),
                NetworkId = ReadString(item, "network_id")
            };

            route.PolicyName = ExtractPolicyName(route.Name, route.Description);

            route.NetworkGroup = wanNetworks.FirstOrDefault(pair => string.Equals(pair.Value.Id, route.NetworkId, StringComparison.OrdinalIgnoreCase)).Key;

            if (item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty("target_devices", out var targetDevices) &&
                targetDevices.ValueKind == JsonValueKind.Array)
            {
                foreach (var target in targetDevices.EnumerateArray())
                {
                    var mac = ReadString(target, "client_mac");
                    var routeTarget = new TrafficRouteTarget
                    {
                        ClientMac = mac
                    };
                    route.TargetDevices.Add(routeTarget);
                }
            }

            routes.Add(route);
        }

        return routes;
    }

    private static async Task<Dictionary<string, QosRuleInfo>> GetQosRuleMapAsync(
        HttpClient client,
        string site,
        IReadOnlyDictionary<string, WanMapping> wanNetworks,
        CancellationToken cancellationToken)
    {
        using var document = await GetJsonAsync(client, $"/proxy/network/v2/api/site/{site}/qos-rules", cancellationToken);
        var result = new Dictionary<string, QosRuleInfo>(StringComparer.OrdinalIgnoreCase);
        if (document is null)
        {
            return result;
        }

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var rule = new QosRuleInfo
            {
                Id = ReadString(item, "_id", "id"),
                Name = ReadString(item, "name"),
                Enabled = ReadBoolean(item, "enabled"),
                Index = ReadInteger(item, "index"),
                WanNetworkId = ReadString(item, "wan_or_vpn_network"),
                DownloadLimitKbps = ReadInteger(item, "download_limit_kbps"),
                UploadLimitKbps = ReadInteger(item, "upload_limit_kbps")
            };

            if (item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty("source", out var source) &&
                source.ValueKind == JsonValueKind.Object &&
                source.TryGetProperty("client_macs", out var clientMacs) &&
                clientMacs.ValueKind == JsonValueKind.Array)
            {
                foreach (var clientMac in clientMacs.EnumerateArray())
                {
                    var mac = clientMac.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(mac))
                    {
                        rule.TargetClientMacs.Add(mac);
                    }
                }
            }

            rule.WanName = wanNetworks.Values.FirstOrDefault(pair => string.Equals(pair.Id, rule.WanNetworkId, StringComparison.OrdinalIgnoreCase))?.Name ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(rule.Id))
            {
                result[rule.Id] = rule;
            }
        }

        return result;
    }

    private static string ResolveSite(string configuredSite, IReadOnlyList<string> availableSites)
    {
        if (!string.IsNullOrWhiteSpace(configuredSite) &&
            availableSites.Any(site => string.Equals(site, configuredSite, StringComparison.OrdinalIgnoreCase)))
        {
            return configuredSite;
        }

        return availableSites.FirstOrDefault() ?? configuredSite;
    }

    private static UniFiClient MapClient(
        JsonElement item,
        IReadOnlyDictionary<string, ClientMetadata> metadata,
        IReadOnlyDictionary<string, TrafficRouteInfo> trafficRoutes,
        IReadOnlyDictionary<string, QosRuleInfo> qosRules,
        string userId,
        int index)
    {
        var id = ReadString(item, "id", "_id", "mac");
        var mac = ReadString(item, "mac");
        var metadataEntry = GetClientMetadata(metadata, id, mac);
        trafficRoutes.TryGetValue(mac, out var route);
        var qosRule = qosRules.Values.FirstOrDefault(rule =>
            rule.TargetClientMacs.Any(target => string.Equals(target, mac, StringComparison.OrdinalIgnoreCase)) &&
            IsUnifiPlusQosRule(rule));
        var policyName = FirstNonEmpty(metadataEntry?.Note ?? string.Empty, ReadString(item, "policyName", "network_policy_name"));
        var hostname = ReadString(item, "hostname");
        var manufacturer = ReadString(item, "oui", "vendor_name", "manufacturer");
        var clientName = ReadString(item, "name", "hostname", "displayName", "mac");
        var ip = ReadString(item, "ipAddress", "ip", "fixed_ip");
        var connectionType = ResolveConnectionType(item);
        var lastSeenUtc = ReadUnixTime(item, "last_seen", "lastSeen");
        var isOnline = ReadBoolean(item, "isOnline", "online") || (lastSeenUtc.HasValue && lastSeenUtc.Value > DateTimeOffset.UtcNow.AddMinutes(-10));

        var assigned = policyName.StartsWith("UP-", StringComparison.OrdinalIgnoreCase) &&
            policyName.Contains($"UP-{userId}-", StringComparison.OrdinalIgnoreCase);

        return new UniFiClient
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(clientName) ? $"Client {index + 1}" : clientName,
            Hostname = hostname,
            IpAddress = ip,
            PolicyName = policyName.StartsWith("UP-", StringComparison.OrdinalIgnoreCase) ? policyName : string.Empty,
            AssignedToCurrentUser = assigned,
            MacAddress = mac,
            Manufacturer = manufacturer,
            ConnectionType = connectionType,
            IsOnline = isOnline,
            LastSeenUtc = lastSeenUtc,
            SelectedWanId = route?.NetworkGroup ?? string.Empty,
            BandwidthRuleId = qosRule?.Id ?? string.Empty,
            BandwidthRuleName = qosRule?.Name ?? string.Empty,
            DownloadLimitMbps = ConvertKbpsToMbps(qosRule?.DownloadLimitKbps),
            UploadLimitMbps = ConvertKbpsToMbps(qosRule?.UploadLimitKbps)
        };
    }

    private static WanInterface MapWan(JsonElement item)
    {
        var id = ReadString(item, "id", "_id", "name");
        var name = ReadString(item, "displayName", "name", "interface", "type");
        var active = ReadBoolean(item, "isActive", "active", "enabled", "primary");

        return new WanInterface
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(name) ? id : name,
            IsActive = active
        };
    }

    private static WanInterface MapWan(JsonElement item, IReadOnlyDictionary<string, WanMapping> wanNetworks)
    {
        var mapped = MapWan(item);
        var resolvedKey = ResolveWanKey(mapped.Id, mapped.Name, wanNetworks);
        if (!string.IsNullOrWhiteSpace(resolvedKey) && wanNetworks.TryGetValue(resolvedKey, out var wan))
        {
            return new WanInterface
            {
                Id = resolvedKey,
                Name = wan.Name,
                IsActive = mapped.IsActive
            };
        }

        return new WanInterface
        {
            Id = NormalizeWanAlias(mapped.Id, mapped.Name),
            Name = ResolveWanDisplayName(NormalizeWanAlias(mapped.Id, mapped.Name), wanNetworks),
            IsActive = mapped.IsActive
        };
    }

    private static async Task<JsonDocument?> GetJsonAsync(HttpClient client, string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest or HttpStatusCode.MethodNotAllowed)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        var trimmed = payload.TrimStart();
        if (!string.IsNullOrWhiteSpace(mediaType) &&
            !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase) &&
            trimmed.Length > 0 &&
            trimmed[0] == '<')
        {
            return null;
        }

        if (trimmed.Length > 0 && trimmed[0] == '<')
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<JsonElement> ExtractArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray();
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                return data.EnumerateArray();
            }

            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                return items.EnumerateArray();
            }
        }

        return [];
    }

    private static List<JsonElement> ExtractElements(JsonDocument? document)
    {
        return document is null ? [] : ExtractArray(document.RootElement).Select(item => item.Clone()).ToList();
    }

    private static async Task<Dictionary<string, ClientMetadata>> GetClientMetadataAsync(
        HttpClient client,
        string site,
        CancellationToken cancellationToken)
    {
        using var document = await GetJsonAsync(client, $"/proxy/network/api/s/{site}/rest/user", cancellationToken);
        var result = new Dictionary<string, ClientMetadata>(StringComparer.OrdinalIgnoreCase);
        if (document is null)
        {
            return result;
        }

        foreach (var item in ExtractArray(document.RootElement))
        {
            var metadata = new ClientMetadata
            {
                Id = ReadString(item, "_id", "id"),
                MacAddress = ReadString(item, "mac"),
                Note = ReadString(item, "note"),
                DisplayName = FirstNonEmpty(ReadString(item, "name", "hostname"), ReadString(item, "mac"))
            };

            if (!string.IsNullOrWhiteSpace(metadata.Id))
            {
                result[metadata.Id] = metadata;
            }

            if (!string.IsNullOrWhiteSpace(metadata.MacAddress))
            {
                result[metadata.MacAddress] = metadata;
            }
        }

        return result;
    }

    private static void DisposeAll(IEnumerable<JsonDocument?> documents)
    {
        foreach (var document in documents)
        {
            document?.Dispose();
        }
    }

    private static ClientMetadata? GetClientMetadata(
        IReadOnlyDictionary<string, ClientMetadata> metadata,
        string id,
        string mac)
    {
        if (!string.IsNullOrWhiteSpace(id) && metadata.TryGetValue(id, out var byId))
        {
            return byId;
        }

        if (!string.IsNullOrWhiteSpace(mac) && metadata.TryGetValue(mac, out var byMac))
        {
            return byMac;
        }

        return null;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static int TryParseClaimIndex(string note, string userId)
    {
        var prefix = $"UP-{userId}-";
        if (!note.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var suffix = note[prefix.Length..];
        return int.TryParse(suffix, out var index) ? index : 0;
    }

    private static string TryExtractUserIdFromPolicy(string note)
    {
        if (string.IsNullOrWhiteSpace(note) || !note.StartsWith("UP-", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var parts = note.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 ? parts[1] : string.Empty;
    }

    private static string BuildBandwidthRuleName(string policyName)
    {
        return policyName.StartsWith("UP-", StringComparison.OrdinalIgnoreCase)
            ? $"{policyName} | Bandwidth"
            : $"{policyName} | Bandwidth";
    }

    private static string ReadString(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var property))
            {
                if (property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString() ?? string.Empty;
                }

                if (property.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                {
                    return property.ToString();
                }
            }
        }

        return string.Empty;
    }

    private static bool ReadBoolean(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var property))
            {
                if (property.ValueKind == JsonValueKind.True)
                {
                    return true;
                }

                if (property.ValueKind == JsonValueKind.False)
                {
                    return false;
                }
            }
        }

        return false;
    }

    private static int ReadInteger(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty(name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var intValue))
            {
                return intValue;
            }

            if (property.ValueKind == JsonValueKind.String &&
                int.TryParse(property.GetString(), out intValue))
            {
                return intValue;
            }
        }

        return -1;
    }

    private static int? ConvertKbpsToMbps(int? value)
    {
        if (!value.HasValue || value.Value <= 0)
        {
            return null;
        }

        return (int)Math.Ceiling(value.Value / 1000d);
    }

    private static DateTimeOffset? ReadUnixTime(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty(name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds);
            }

            if (property.ValueKind == JsonValueKind.String &&
                long.TryParse(property.GetString(), out seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds);
            }
        }

        return null;
    }

    private static string ResolveConnectionType(JsonElement item)
    {
        if (ReadBoolean(item, "is_wired", "wired"))
        {
            return "Wired";
        }

        var essid = ReadString(item, "essid");
        var radio = ReadString(item, "radio", "channel");
        if (!string.IsNullOrWhiteSpace(essid) || !string.IsNullOrWhiteSpace(radio))
        {
            return "WiFi";
        }

        var type = ReadString(item, "type");
        return type switch
        {
            "wired" => "Wired",
            "wireless" => "WiFi",
            _ => "Other"
        };
    }

    private static async Task<List<JsonElement>> GetLegacyWansRawAsync(HttpClient client, string site, CancellationToken cancellationToken)
    {
        using var deviceDocument = await GetJsonAsync(client, $"/proxy/network/api/s/{site}/stat/device", cancellationToken);
        using var healthDocument = await GetJsonAsync(client, $"/proxy/network/api/s/{site}/stat/health", cancellationToken);

        var results = new List<JsonElement>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string activeWanId = string.Empty;
        JsonElement gateway = default;
        if (TryGetGatewayDevice(deviceDocument, out gateway))
        {
            activeWanId = ReadString(ReadObject(gateway, "uplink"), "comment");

            AddLegacyWan(results, seen, "WAN", BuildLegacyWan(gateway, "WAN", "wan1"));
            AddLegacyWan(results, seen, "WAN2", BuildLegacyWan(gateway, "WAN2", "wan2"));
        }

        if (TryGetHealthWan(healthDocument, out var healthWan))
        {
            var uptimeStats = ReadObject(healthWan, "uptime_stats");
            foreach (var property in uptimeStats.EnumerateObject())
            {
                var key = property.Name;
                if (seen.Contains(key))
                {
                    continue;
                }

                var fallbackName = key switch
                {
                    "WAN_LTE_FAILOVER" => "LTE Failover WAN",
                    "WAN" => "Internet Vodafone",
                    "WAN2" => "Internet O2",
                    _ => key.Replace('_', ' ')
                };

                var element = JsonSerializer.SerializeToElement(new
                {
                    id = key,
                    name = fallbackName,
                    active = string.Equals(activeWanId, key, StringComparison.OrdinalIgnoreCase)
                });

                AddLegacyWan(results, seen, key, element);
            }
        }

        return results;
    }

    private static void AddLegacyWan(List<JsonElement> results, HashSet<string> seen, string id, JsonElement element)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        if (!seen.Add(id))
        {
            return;
        }

        results.Add(element);
    }

    private static JsonElement BuildLegacyWan(JsonElement gateway, string wanId, string gatewayPropertyName)
    {
        var wan = ReadObject(gateway, gatewayPropertyName);
        if (wan.ValueKind == JsonValueKind.Undefined)
        {
            return default;
        }

        var displayName = wanId switch
        {
            "WAN" => "Internet Vodafone",
            "WAN2" => "Internet O2",
            "WAN_LTE_FAILOVER" => "LTE Failover WAN",
            _ => wanId
        };

        return JsonSerializer.SerializeToElement(new
        {
            id = wanId,
            name = displayName,
            active = string.Equals(ReadString(ReadObject(gateway, "uplink"), "comment"), wanId, StringComparison.OrdinalIgnoreCase)
        });
    }

    private static bool TryGetGatewayDevice(JsonDocument? deviceDocument, out JsonElement gateway)
    {
        gateway = default;
        if (deviceDocument is null)
        {
            return false;
        }

        foreach (var item in ExtractArray(deviceDocument.RootElement))
        {
            if (string.Equals(ReadString(item, "type"), "ugw", StringComparison.OrdinalIgnoreCase))
            {
                gateway = item;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetHealthWan(JsonDocument? healthDocument, out JsonElement healthWan)
    {
        healthWan = default;
        if (healthDocument is null)
        {
            return false;
        }

        foreach (var item in ExtractArray(healthDocument.RootElement))
        {
            if (string.Equals(ReadString(item, "subsystem"), "wan", StringComparison.OrdinalIgnoreCase))
            {
                healthWan = item;
                return true;
            }
        }

        return false;
    }

    private static JsonElement ReadObject(JsonElement item, string name)
    {
        if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Object)
        {
            return property;
        }

        return default;
    }

    private static string ResolveWanDisplayName(string wanId, IReadOnlyDictionary<string, WanMapping> wanNetworks)
    {
        var resolvedKey = ResolveWanKey(wanId, string.Empty, wanNetworks);
        if (!string.IsNullOrWhiteSpace(resolvedKey) && wanNetworks.TryGetValue(resolvedKey, out var wan))
        {
            return wan.Name;
        }

        return NormalizeWanAlias(wanId, string.Empty) switch
        {
            "WAN" => "Internet Vodafone",
            "WAN2" => "Internet O2",
            "WAN_LTE_FAILOVER" => "LTE Failover WAN",
            _ => wanId
        };
    }

    private static string ResolveWanKey(
        string wanId,
        string wanName,
        IReadOnlyDictionary<string, WanMapping> wanNetworks)
    {
        var normalized = NormalizeWanAlias(wanId, wanName);
        if (!string.IsNullOrWhiteSpace(normalized) && wanNetworks.ContainsKey(normalized))
        {
            return normalized;
        }

        foreach (var pair in wanNetworks)
        {
            if (string.Equals(pair.Key, wanId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Value.Id, wanId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Value.Name, wanName, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Key;
            }
        }

        return normalized;
    }

    private static string NormalizeWanAlias(string wanId, string wanName)
    {
        var candidate = FirstNonEmpty(wanId, wanName).Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return string.Empty;
        }

        return candidate.ToUpperInvariant() switch
        {
            "WAN1" => "WAN",
            "WAN 1" => "WAN",
            "WAN" => "WAN",
            "INTERNET VODAFONE" => "WAN",
            "WAN2" => "WAN2",
            "WAN 2" => "WAN2",
            "INTERNET O2" => "WAN2",
            "WAN_LTE_FAILOVER" => "WAN_LTE_FAILOVER",
            "LTE FAILOVER WAN" => "WAN_LTE_FAILOVER",
            _ => candidate
        };
    }

    private static string ExtractPolicyName(string routeName, string description)
    {
        if (!string.IsNullOrWhiteSpace(routeName))
        {
            var routePrefix = routeName.Split(" -> ", 2, StringSplitOptions.None)[0];
            if (routePrefix.StartsWith("UP-", StringComparison.OrdinalIgnoreCase))
            {
                return routePrefix;
            }
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            var marker = "UnifiPlus route for ";
            var start = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start >= 0)
            {
                var remaining = description[(start + marker.Length)..];
                var end = remaining.IndexOf(" (", StringComparison.OrdinalIgnoreCase);
                return end >= 0 ? remaining[..end] : remaining;
            }
        }

        return string.Empty;
    }

    private static bool IsUnifiPlusRoute(TrafficRouteInfo route)
    {
        return route.PolicyName.StartsWith("UP-", StringComparison.OrdinalIgnoreCase) ||
            route.Name.StartsWith("UP-", StringComparison.OrdinalIgnoreCase) ||
            route.Description.StartsWith("UnifiPlus route for UP-", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveBandwidthWanNetworkId(
        string clientMac,
        IReadOnlyDictionary<string, TrafficRouteInfo> trafficRoutes,
        IReadOnlyDictionary<string, WanMapping> wanNetworks)
    {
        if (trafficRoutes.TryGetValue(clientMac, out var route) &&
            !string.IsNullOrWhiteSpace(route.NetworkId))
        {
            return route.NetworkId;
        }

        return wanNetworks.Values.FirstOrDefault().Id;
    }

    private static bool IsUnifiPlusQosRule(QosRuleInfo rule)
    {
        return rule.Name.StartsWith("UP-", StringComparison.OrdinalIgnoreCase) &&
            rule.Name.Contains("| Bandwidth", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatBandwidthConfiguration(QosRuleInfo rule, IReadOnlyDictionary<string, WanMapping> wanNetworks)
    {
        var parts = new List<string>();
        if (rule.DownloadLimitKbps > 0)
        {
            parts.Add($"Down {ConvertKbpsToMbps(rule.DownloadLimitKbps)} Mbps");
        }

        if (rule.UploadLimitKbps > 0)
        {
            parts.Add($"Up {ConvertKbpsToMbps(rule.UploadLimitKbps)} Mbps");
        }

        var limitText = parts.Count == 0 ? "No limit" : string.Join(" / ", parts);
        var wanName = !string.IsNullOrWhiteSpace(rule.WanName)
            ? rule.WanName
            : wanNetworks.Values.FirstOrDefault(item => string.Equals(item.Id, rule.WanNetworkId, StringComparison.OrdinalIgnoreCase))?.Name;

        return string.IsNullOrWhiteSpace(wanName) ? limitText : $"{limitText} on {wanName}";
    }

    private sealed class UniFiSession : IDisposable
    {
        public HttpClient Client { get; init; } = default!;

        public string Site { get; init; } = string.Empty;

        public string EndpointRoot { get; init; } = string.Empty;

        public bool Authenticated { get; init; }

        public void Dispose()
        {
            Client.Dispose();
        }
    }

    private sealed class ClientMetadata
    {
        public string Id { get; init; } = string.Empty;

        public string MacAddress { get; init; } = string.Empty;

        public string Note { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

    }

    private sealed class QosRuleInfo
    {
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public bool Enabled { get; init; }

        public int Index { get; init; }

        public string WanNetworkId { get; init; } = string.Empty;

        public string WanName { get; set; } = string.Empty;

        public int DownloadLimitKbps { get; init; }

        public int UploadLimitKbps { get; init; }

        public List<string> TargetClientMacs { get; init; } = [];
    }

    private sealed class WanMapping
    {
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Purpose { get; init; } = string.Empty;
    }

    private sealed class TrafficRouteInfo
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string PolicyName { get; set; } = string.Empty;

        public string NetworkId { get; set; } = string.Empty;

        public string NetworkGroup { get; set; } = string.Empty;

        public List<TrafficRouteTarget> TargetDevices { get; set; } = [];
    }

    private sealed class TrafficRouteTarget
    {
        public string ClientMac { get; set; } = string.Empty;
    }
}
