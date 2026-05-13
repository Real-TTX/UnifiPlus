using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiPlus.Web.Models;
using UnifiPlus.Web.Services;

namespace UnifiPlus.Web.Controllers;

public sealed class AccountController : Controller
{
    private const string ApiKeyStatusKey = "ApiKeyStatus";
    private const string ApiKeyPlaintextKey = "ApiKeyPlaintext";
    private readonly IUniFiClientAssignmentService _assignmentService;
    private readonly ILocalIdentityService _localIdentityService;
    private readonly ILocalUserManagementService _localUserManagementService;
    private readonly IIdentityBootstrapService _bootstrapService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IBandwidthTemplateStore _bandwidthTemplateStore;

    public AccountController(
        IUniFiClientAssignmentService assignmentService,
        ILocalIdentityService localIdentityService,
        ILocalUserManagementService localUserManagementService,
        IIdentityBootstrapService bootstrapService,
        IApiKeyService apiKeyService,
        IBandwidthTemplateStore bandwidthTemplateStore)
    {
        _assignmentService = assignmentService;
        _localIdentityService = localIdentityService;
        _localUserManagementService = localUserManagementService;
        _bootstrapService = bootstrapService;
        _apiKeyService = apiKeyService;
        _bandwidthTemplateStore = bandwidthTemplateStore;
    }

    [HttpGet]
    public async Task<IActionResult> Login(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect("/uplink-switcher");
        }

        if (await _bootstrapService.IsAdminSetupRequiredAsync(cancellationToken))
        {
            return RedirectToAction("Admin", "Setup");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _localIdentityService.ValidateAsync(model.UserId, model.Password, HttpContext.RequestAborted);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserId),
            new("unifiplus:user_id", user.UserId),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            if (await _bootstrapService.IsUniFiConnectionSetupRequiredAsync(HttpContext.RequestAborted))
            {
                return RedirectToAction("Connection", "Admin");
            }
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Manage(CancellationToken cancellationToken)
    {
        var model = await BuildAccountViewModelAsync(cancellationToken);
        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Devices(CancellationToken cancellationToken)
    {
        var model = await BuildAccountDevicesViewModelAsync(cancellationToken);
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "PasswordForm")] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var model = await BuildAccountViewModelAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            model = model.WithPassword(request);
            return View("Manage", model);
        }

        try
        {
            await _localUserManagementService.ChangeOwnPasswordAsync(User.Identity?.Name ?? string.Empty, request.CurrentPassword, request.NewPassword, cancellationToken);
            TempData["PasswordChanged"] = "Your password was updated successfully.";
            return RedirectToAction(nameof(Manage));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model = model.WithPassword(request);
            return View("Manage", model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateApiKey([Bind(Prefix = "ApiKeyForm")] CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var model = await BuildAccountViewModelAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("Manage", model.WithApiKeyForm(request));
        }

        var result = await _apiKeyService.CreateAsync(User.Identity?.Name ?? string.Empty, request.Name, cancellationToken);
        TempData[ApiKeyStatusKey] = $"API key '{result.Record.Name}' created successfully.";
        TempData[ApiKeyPlaintextKey] = result.PlaintextKey;
        return RedirectToAction(nameof(Manage));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeApiKey(RevokeApiKeyRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "The API key could not be revoked.";
            return RedirectToAction(nameof(Manage));
        }

        var revoked = await _apiKeyService.RevokeAsync(User.Identity?.Name ?? string.Empty, request.KeyId, cancellationToken);
        TempData["ActionStatus"] = revoked
            ? "API key revoked successfully."
            : "The selected API key could not be found.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClaimDevice(AssignClientRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "The device could not be claimed.";
            return RedirectToAction(nameof(Devices));
        }

        try
        {
            await _assignmentService.AssignClientAsync(User, request.ClientId, cancellationToken);
            TempData["ActionStatus"] = "Device claimed successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ActionError"] = ex.Message;
        }

        return RedirectToAction(nameof(Devices));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDeviceWan(UpdateWanRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "The uplink could not be updated.";
            return RedirectToAction(nameof(Devices));
        }

        try
        {
            await _assignmentService.UpdateWanAsync(User, request.ClientId, request.WanId, cancellationToken);
            TempData["ActionStatus"] = "Uplink switch applied successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ActionError"] = ex.Message;
        }

        return RedirectToAction(nameof(Devices));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDeviceBandwidth(UpdateBandwidthRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "Enter at least one valid upload or download limit.";
            return RedirectToAction(nameof(Devices));
        }

        try
        {
            await _assignmentService.UpdateBandwidthAsync(User, request.ClientId, request.DownloadLimitMbps, request.UploadLimitMbps, cancellationToken);
            TempData["ActionStatus"] = "Bandwidth limit applied successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ActionError"] = ex.Message;
        }

        return RedirectToAction(nameof(Devices));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetClientAlias(SetClientAliasRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "The client alias could not be saved.";
            return RedirectToAction(nameof(Devices));
        }

        try
        {
            await _localUserManagementService.SetClientAliasAsync(User.Identity?.Name ?? string.Empty, request.ClientId, request.Alias, cancellationToken);
            TempData["ActionStatus"] = string.IsNullOrWhiteSpace(request.Alias)
                ? "Client alias removed successfully."
                : "Client alias saved successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ActionError"] = ex.Message;
        }

        return RedirectToAction(nameof(Devices));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task<AccountViewModel> BuildAccountViewModelAsync(CancellationToken cancellationToken)
    {
        var model = await _assignmentService.BuildAccountAsync(User, cancellationToken);
        var apiKeys = await _apiKeyService.GetForUserAsync(User.Identity?.Name ?? string.Empty, cancellationToken);

        return new AccountViewModel
        {
            UserId = model.UserId,
            Role = model.Role,
            AvailableClients = model.AvailableClients,
            AvailableWans = model.AvailableWans,
            AssignedClients = model.AssignedClients,
            PasswordForm = model.PasswordForm,
            ApiKeyForm = model.ApiKeyForm,
            ApiKeys = apiKeys
                .Select(key => new ApiKeyListItemViewModel
                {
                    Id = key.Id,
                    Name = key.Name,
                    KeyPrefix = key.KeyPrefix,
                    CreatedUtc = key.CreatedUtc,
                    LastUsedUtc = key.LastUsedUtc,
                    IsRevoked = key.IsRevoked
                })
                .ToList()
        };
    }

    private async Task<AccountDevicesViewModel> BuildAccountDevicesViewModelAsync(CancellationToken cancellationToken)
    {
        var dashboard = await _assignmentService.BuildDashboardAsync(User, cancellationToken);
        var templates = await _bandwidthTemplateStore.GetAsync(cancellationToken);

        return new AccountDevicesViewModel
        {
            UserId = dashboard.UserId,
            AvailableClients = dashboard.AllClients,
            AvailableWans = dashboard.AvailableWans,
            AssignedClients = dashboard.AssignedClients,
            DownloadTemplateValuesMbps = templates.DownloadTemplatesMbps
                .Where(value => value > 0)
                .Distinct()
                .OrderBy(value => value)
                .ToList(),
            UploadTemplateValuesMbps = templates.UploadTemplatesMbps
                .Where(value => value > 0)
                .Distinct()
                .OrderBy(value => value)
                .ToList()
        };
    }
}
