using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiPlus.Web.Authorization;
using UnifiPlus.Web.Models;
using UnifiPlus.Web.Services;

namespace UnifiPlus.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public sealed class AdminController : Controller
{
    private const string ConnectionStatusKey = "ConnectionStatus";
    private const string ConnectionStatusSuccessKey = "ConnectionStatusIsSuccess";
    private const string UsersStatusKey = "UsersStatus";
    private const string UsersStatusSuccessKey = "UsersStatusIsSuccess";
    private const string TemplatesStatusKey = "TemplatesStatus";
    private const string TemplatesStatusSuccessKey = "TemplatesStatusIsSuccess";
    private readonly IAdminSetupService _adminSetupService;
    private readonly IUniFiApiClient _uniFiApiClient;
    private readonly ILocalUserManagementService _localUserManagementService;
    private readonly IBandwidthTemplateStore _bandwidthTemplateStore;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAdminSetupService adminSetupService,
        IUniFiApiClient uniFiApiClient,
        ILocalUserManagementService localUserManagementService,
        IBandwidthTemplateStore bandwidthTemplateStore,
        ILogger<AdminController> logger)
    {
        _adminSetupService = adminSetupService;
        _uniFiApiClient = uniFiApiClient;
        _localUserManagementService = localUserManagementService;
        _bandwidthTemplateStore = bandwidthTemplateStore;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Connection(CancellationToken cancellationToken)
    {
        var model = await _adminSetupService.BuildViewModelAsync(cancellationToken);
        if (TempData[ConnectionStatusKey] is string statusMessage)
        {
            model = model.WithStatus(statusMessage, TempData[ConnectionStatusSuccessKey] as string == bool.TrueString);
        }

        if (model.HasSavedConfiguration)
        {
            var test = await _adminSetupService.TestConnectionAsync(model.Form, cancellationToken);
            model = model.WithTest(test);
        }

        return View("Connection", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recover(CancellationToken cancellationToken)
    {
        var model = await _adminSetupService.BuildViewModelAsync(cancellationToken);
        if (model.HasSavedConfiguration)
        {
            var test = await _adminSetupService.TestConnectionAsync(model.Form, cancellationToken);
            model = model.WithTest(test);
        }

        try
        {
            var snapshot = await _uniFiApiClient.GetRecoverySnapshotAsync(cancellationToken);
            var recoveredUsers = await _localUserManagementService.RecoverUsersFromUniFiAsync(snapshot.UserIds, cancellationToken);
            model = model
                .WithStatus("Recovery from UniFi completed.", true)
                .WithRecovery(new UniFiRecoveryResult
                {
                    Snapshot = snapshot,
                    Users = recoveredUsers
                });
        }
        catch (Exception ex)
        {
            model = model.WithStatus($"Recovery from UniFi failed: {ex.Message}", false);
        }

        return View("Connection", model);
    }

    [HttpGet]
    public async Task<IActionResult> EditConnection(CancellationToken cancellationToken)
    {
        var model = await _adminSetupService.BuildViewModelAsync(cancellationToken);
        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminUniFiSetupRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Admin Save called. BaseUrl={BaseUrl}, Site={Site}, ApiKeyProvided={ApiKeyProvided}, Username={Username}, PasswordProvided={PasswordProvided}, AllowSelfSignedTls={AllowSelfSignedTls}, ModelStateValid={ModelStateValid}",
            request.BaseUrl,
            request.Site,
            !string.IsNullOrWhiteSpace(request.ApiKey),
            request.Username,
            !string.IsNullOrWhiteSpace(request.Password),
            request.AllowSelfSignedTls,
            ModelState.IsValid);

        if (!ModelState.IsValid)
        {
            foreach (var entry in ModelState)
            {
                foreach (var error in entry.Value.Errors)
                {
                    _logger.LogWarning("Admin Save validation error on {Field}: {Error}", entry.Key, error.ErrorMessage);
                }
            }
        }

        var baseModel = await _adminSetupService.BuildViewModelAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("Index", baseModel.WithForm(request));
        }

        await _adminSetupService.SaveConfigurationAsync(request, cancellationToken);
        var result = await _adminSetupService.TestConnectionAsync(request, cancellationToken);
        TempData[ConnectionStatusKey] = result.Success
            ? "UniFi connection settings saved and validated successfully."
            : "UniFi connection settings saved, but validation still needs attention.";
        TempData[ConnectionStatusSuccessKey] = result.Success.ToString();
        return RedirectToAction(nameof(Connection));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Test(AdminUniFiSetupRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Admin Test called. BaseUrl={BaseUrl}, Site={Site}, ApiKeyProvided={ApiKeyProvided}, Username={Username}, PasswordProvided={PasswordProvided}, AllowSelfSignedTls={AllowSelfSignedTls}, ModelStateValid={ModelStateValid}",
            request.BaseUrl,
            request.Site,
            !string.IsNullOrWhiteSpace(request.ApiKey),
            request.Username,
            !string.IsNullOrWhiteSpace(request.Password),
            request.AllowSelfSignedTls,
            ModelState.IsValid);

        var model = await _adminSetupService.BuildViewModelAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("Index", model.WithForm(request));
        }

        var result = await _adminSetupService.TestConnectionAsync(request, cancellationToken);
        return View("Index", model.WithForm(request).WithTest(result));
    }

    [HttpGet]
    public async Task<IActionResult> Rules(CancellationToken cancellationToken)
    {
        var rules = await _uniFiApiClient.GetActiveRulesAsync(cancellationToken);
        return View(new ActiveRulesPageViewModel
        {
            Rules = rules,
            StatusMessage = TempData["RulesStatus"] as string,
            StatusIsSuccess = TempData["RulesStatusIsSuccess"] as string == bool.TrueString
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRule(string ruleId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            TempData["RulesStatus"] = "No rule id was provided.";
            TempData["RulesStatusIsSuccess"] = bool.FalseString;
            return RedirectToAction(nameof(Rules));
        }

        try
        {
            await _uniFiApiClient.DeleteActiveRuleAsync(ruleId, cancellationToken);
            TempData["RulesStatus"] = "Rule deleted successfully.";
            TempData["RulesStatusIsSuccess"] = bool.TrueString;
        }
        catch (Exception ex)
        {
            TempData["RulesStatus"] = $"Deleting the rule failed: {ex.Message}";
            TempData["RulesStatusIsSuccess"] = bool.FalseString;
        }

        return RedirectToAction(nameof(Rules));
    }

    [HttpGet]
    public async Task<IActionResult> Templates(CancellationToken cancellationToken)
    {
        var settings = await _bandwidthTemplateStore.GetAsync(cancellationToken);
        return View(new BandwidthTemplatesViewModel
        {
            DownloadTemplatesMbps = settings.DownloadTemplatesMbps
                .Where(value => value > 0)
                .Distinct()
                .OrderBy(value => value)
                .ToList(),
            UploadTemplatesMbps = settings.UploadTemplatesMbps
                .Where(value => value > 0)
                .Distinct()
                .OrderBy(value => value)
                .ToList(),
            StoragePath = _bandwidthTemplateStore.StoragePath,
            LastUpdatedUtc = settings.LastUpdatedUtc,
            StatusMessage = TempData[TemplatesStatusKey] as string,
            StatusIsSuccess = TempData[TemplatesStatusSuccessKey] as string == bool.TrueString
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTemplates([Bind(Prefix = "Form")] BandwidthTemplateSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var current = await _bandwidthTemplateStore.GetAsync(cancellationToken);
            return View("EditTemplate", BuildEditTemplateViewModel(request, current));
        }

        var currentSettings = await _bandwidthTemplateStore.GetAsync(cancellationToken);
        var values = request.TemplatesCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToList();

        var nextSettings = new BandwidthTemplateSettings
        {
            DownloadTemplatesMbps = string.Equals(request.TemplateType, "Download", StringComparison.OrdinalIgnoreCase)
                ? values
                : currentSettings.DownloadTemplatesMbps,
            UploadTemplatesMbps = string.Equals(request.TemplateType, "Upload", StringComparison.OrdinalIgnoreCase)
                ? values
                : currentSettings.UploadTemplatesMbps
        };

        await _bandwidthTemplateStore.SaveAsync(nextSettings, cancellationToken);

        TempData[TemplatesStatusKey] = $"{request.TemplateType} templates saved successfully.";
        TempData[TemplatesStatusSuccessKey] = bool.TrueString;
        return RedirectToAction(nameof(Templates));
    }

    [HttpGet]
    public async Task<IActionResult> EditTemplate(string type, CancellationToken cancellationToken)
    {
        var settings = await _bandwidthTemplateStore.GetAsync(cancellationToken);
        var normalizedType = NormalizeTemplateType(type);
        var request = new BandwidthTemplateSettingsRequest
        {
            TemplateType = normalizedType,
            TemplatesCsv = string.Join(", ", string.Equals(normalizedType, "Upload", StringComparison.OrdinalIgnoreCase)
                ? settings.UploadTemplatesMbps
                : settings.DownloadTemplatesMbps)
        };

        return View(BuildEditTemplateViewModel(request, settings));
    }

    private EditBandwidthTemplateViewModel BuildEditTemplateViewModel(
        BandwidthTemplateSettingsRequest request,
        BandwidthTemplateSettings settings)
    {
        var normalizedType = NormalizeTemplateType(request.TemplateType);
        var isUpload = string.Equals(normalizedType, "Upload", StringComparison.OrdinalIgnoreCase);
        var currentValues = (isUpload ? settings.UploadTemplatesMbps : settings.DownloadTemplatesMbps)
            .Where(value => value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToList();

        return new EditBandwidthTemplateViewModel
        {
            Form = new BandwidthTemplateSettingsRequest
            {
                TemplateType = normalizedType,
                TemplatesCsv = request.TemplatesCsv
            },
            Title = isUpload ? "Upload templates" : "Download templates",
            Description = isUpload
                ? "Edit the reusable upload limit presets used on the Bandwidth Limiter."
                : "Edit the reusable download limit presets used on the Bandwidth Limiter.",
            CurrentTemplatesMbps = currentValues,
            StoragePath = _bandwidthTemplateStore.StoragePath,
            LastUpdatedUtc = settings.LastUpdatedUtc
        };
    }

    private static string NormalizeTemplateType(string? type)
    {
        return string.Equals(type, "Upload", StringComparison.OrdinalIgnoreCase)
            ? "Upload"
            : "Download";
    }

    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var users = await _localUserManagementService.GetUsersAsync(cancellationToken);
        return View(BuildUserManagementViewModel(users));
    }

    [HttpGet]
    public IActionResult AddUser()
    {
        return View(new LocalUserCreateRequest());
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction(nameof(Users));
        }

        var users = await _localUserManagementService.GetUsersAsync(cancellationToken);
        var user = users.FirstOrDefault(item => string.Equals(item.UserId, userId, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            TempData[UsersStatusKey] = "The selected user could not be found.";
            TempData[UsersStatusSuccessKey] = bool.FalseString;
            return RedirectToAction(nameof(Users));
        }

        return View(BuildUserEditViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(LocalUserCreateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("AddUser", request);
        }

        try
        {
            await _localUserManagementService.CreateUserAsync(request.UserId, request.Password, request.Role, cancellationToken);
            TempData[UsersStatusKey] = "User created successfully.";
            TempData[UsersStatusSuccessKey] = bool.TrueString;
        }
        catch (Exception ex)
        {
            TempData[UsersStatusKey] = ex.Message;
            TempData[UsersStatusSuccessKey] = bool.FalseString;
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRole(AdminUserRoleUpdateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _localUserManagementService.ChangeRoleAsync(request.UserId, request.Role, User.Identity?.Name ?? string.Empty, cancellationToken);
            TempData[UsersStatusKey] = "User role updated successfully.";
            TempData[UsersStatusSuccessKey] = bool.TrueString;
        }
        catch (Exception ex)
        {
            TempData[UsersStatusKey] = ex.Message;
            TempData[UsersStatusSuccessKey] = bool.FalseString;
        }

        return RedirectToAction(nameof(EditUser), new { userId = request.UserId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetUserPassword(AdminUserPasswordResetRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData[UsersStatusKey] = "The new password could not be validated.";
            TempData[UsersStatusSuccessKey] = bool.FalseString;
            return RedirectToAction(nameof(EditUser), new { userId = request.UserId });
        }

        try
        {
            await _localUserManagementService.ResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
            TempData[UsersStatusKey] = "Password reset successfully.";
            TempData[UsersStatusSuccessKey] = bool.TrueString;
        }
        catch (Exception ex)
        {
            TempData[UsersStatusKey] = ex.Message;
            TempData[UsersStatusSuccessKey] = bool.FalseString;
        }

        return RedirectToAction(nameof(EditUser), new { userId = request.UserId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(AdminUserDeleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _localUserManagementService.DeleteUserAsync(request.UserId, User.Identity?.Name ?? string.Empty, cancellationToken);
            TempData[UsersStatusKey] = "User deleted successfully.";
            TempData[UsersStatusSuccessKey] = bool.TrueString;
        }
        catch (Exception ex)
        {
            TempData[UsersStatusKey] = ex.Message;
            TempData[UsersStatusSuccessKey] = bool.FalseString;
            return RedirectToAction(nameof(EditUser), new { userId = request.UserId });
        }

        return RedirectToAction(nameof(Users));
    }

    private UserManagementViewModel BuildUserManagementViewModel(
        IReadOnlyList<Data.LocalUser> users)
    {
        return new UserManagementViewModel
        {
            CurrentUserId = User.Identity?.Name ?? string.Empty,
            StatusMessage = TempData[UsersStatusKey] as string,
            StatusIsSuccess = TempData[UsersStatusSuccessKey] as string == bool.TrueString,
            Users = users
                .OrderBy(user => user.UserId, StringComparer.OrdinalIgnoreCase)
                .Select(user => new ManagedUserViewModel
                {
                    UserId = user.UserId,
                    Role = user.Role,
                    CreatedUtc = user.CreatedUtc,
                    IsCurrentUser = string.Equals(user.UserId, User.Identity?.Name, StringComparison.OrdinalIgnoreCase)
                })
                .ToList()
        };
    }

    private UserEditViewModel BuildUserEditViewModel(Data.LocalUser user)
    {
        return new UserEditViewModel
        {
            StatusMessage = TempData[UsersStatusKey] as string,
            StatusIsSuccess = TempData[UsersStatusSuccessKey] as string == bool.TrueString,
            User = new ManagedUserViewModel
            {
                UserId = user.UserId,
                Role = user.Role,
                CreatedUtc = user.CreatedUtc,
                IsCurrentUser = string.Equals(user.UserId, User.Identity?.Name, StringComparison.OrdinalIgnoreCase)
            },
            RoleForm = new AdminUserRoleUpdateRequest
            {
                UserId = user.UserId,
                Role = user.Role
            },
            PasswordForm = new AdminUserPasswordResetRequest
            {
                UserId = user.UserId
            },
            DeleteForm = new AdminUserDeleteRequest
            {
                UserId = user.UserId
            }
        };
    }
}
