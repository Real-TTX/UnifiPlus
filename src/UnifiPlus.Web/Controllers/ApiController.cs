using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiPlus.Web.Authentication;
using UnifiPlus.Web.Models;
using UnifiPlus.Web.Models.Api;
using UnifiPlus.Web.Services;

namespace UnifiPlus.Web.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.SchemeName)]
public sealed class ApiController : ControllerBase
{
    private readonly IUniFiClientAssignmentService _assignmentService;
    private readonly IBandwidthTemplateStore _bandwidthTemplateStore;

    public ApiController(IUniFiClientAssignmentService assignmentService, IBandwidthTemplateStore bandwidthTemplateStore)
    {
        _assignmentService = assignmentService;
        _bandwidthTemplateStore = bandwidthTemplateStore;
    }

    [AllowAnonymous]
    [HttpGet("health")]
    public ActionResult<ApiHealthResponse> Health()
    {
        return Ok(new ApiHealthResponse());
    }

    [HttpGet("me")]
    public ActionResult<ApiCurrentUserResponse> Me()
    {
        return Ok(new ApiCurrentUserResponse
        {
            UserId = User.Identity?.Name ?? string.Empty,
            Role = User.IsInRole("Admin") ? "Admin" : "User",
            IsAdmin = User.IsInRole("Admin")
        });
    }

    [HttpGet("wans")]
    public async Task<ActionResult<IReadOnlyList<WanInterface>>> Wans(CancellationToken cancellationToken)
    {
        var dashboard = await _assignmentService.BuildDashboardAsync(User, cancellationToken);
        return Ok(dashboard.AvailableWans);
    }

    [HttpGet("clients")]
    public async Task<ActionResult<object>> Clients([FromQuery] string scope = "assigned", CancellationToken cancellationToken = default)
    {
        var dashboard = await _assignmentService.BuildDashboardAsync(User, cancellationToken);
        var includeAll = string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase);
        var clients = includeAll
            ? dashboard.AllClients.Select(client => new ApiClientResponse
            {
                ClientId = client.Id,
                Name = client.Name,
                DisplayName = string.IsNullOrWhiteSpace(client.AliasName) ? client.Name : client.AliasName,
                Hostname = client.Hostname,
                IpAddress = client.IpAddress,
                MacAddress = client.MacAddress,
                Manufacturer = client.Manufacturer,
                ConnectionType = client.ConnectionType,
                IsOnline = client.IsOnline,
                PolicyName = client.PolicyName,
                ActiveWanId = client.SelectedWanId,
                ActiveWanName = string.Empty,
                DownloadLimitMbps = client.DownloadLimitMbps,
                UploadLimitMbps = client.UploadLimitMbps
            }).ToList()
            : dashboard.AssignedClients.Select(MapAssignedClient).ToList();

        return Ok(new
        {
            scope = includeAll ? "all" : "assigned",
            count = clients.Count,
            items = clients
        });
    }

    [HttpPost("clients/{clientId}/claim")]
    public async Task<IActionResult> ClaimClient(string clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            ModelState.AddModelError("clientId", "Client id is required.");
            return ValidationProblem(ModelState);
        }

        await _assignmentService.AssignClientAsync(User, clientId, cancellationToken);
        return Ok(new { message = "Device claimed successfully." });
    }

    [HttpPost("clients/{clientId}/uplink")]
    public async Task<IActionResult> UpdateUplink(string clientId, [FromBody] ApiUplinkUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await _assignmentService.UpdateWanAsync(User, clientId, request.WanId, cancellationToken);
        return Ok(new { message = "Uplink switch applied successfully." });
    }

    [HttpPost("clients/{clientId}/bandwidth")]
    public async Task<IActionResult> UpdateBandwidth(string clientId, [FromBody] ApiBandwidthUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!request.DownloadLimitMbps.HasValue && !request.UploadLimitMbps.HasValue)
        {
            ModelState.AddModelError("limits", "At least one upload or download limit is required.");
            return ValidationProblem(ModelState);
        }

        await _assignmentService.UpdateBandwidthAsync(User, clientId, request.DownloadLimitMbps, request.UploadLimitMbps, cancellationToken);
        return Ok(new { message = "Bandwidth limit applied successfully." });
    }

    [HttpGet("bandwidth/templates")]
    public async Task<IActionResult> BandwidthTemplates(CancellationToken cancellationToken)
    {
        var templates = await _bandwidthTemplateStore.GetAsync(cancellationToken);
        return Ok(new
        {
            downloadMbps = templates.DownloadTemplatesMbps.Where(value => value > 0).Distinct().OrderBy(value => value).ToList(),
            uploadMbps = templates.UploadTemplatesMbps.Where(value => value > 0).Distinct().OrderBy(value => value).ToList()
        });
    }

    private static ApiClientResponse MapAssignedClient(AssignedClientViewModel client)
    {
        return new ApiClientResponse
        {
            ClientId = client.ClientId,
            Name = client.ClientName,
            DisplayName = string.IsNullOrWhiteSpace(client.AliasName) ? client.ClientName : client.AliasName,
            Hostname = client.Hostname,
            IpAddress = client.IpAddress,
            MacAddress = client.MacAddress,
            Manufacturer = client.Manufacturer,
            ConnectionType = client.ConnectionType,
            IsOnline = client.IsOnline,
            PolicyName = client.PolicyName,
            ActiveWanId = client.ActiveWanId,
            ActiveWanName = client.ActiveWanName,
            DownloadLimitMbps = client.DownloadLimitMbps,
            UploadLimitMbps = client.UploadLimitMbps
        };
    }
}
