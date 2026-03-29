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
    private readonly IUniFiClientAssignmentService _assignmentService;
    private readonly ILocalIdentityService _localIdentityService;
    private readonly ILocalUserManagementService _localUserManagementService;
    private readonly IIdentityBootstrapService _bootstrapService;

    public AccountController(
        IUniFiClientAssignmentService assignmentService,
        ILocalIdentityService localIdentityService,
        ILocalUserManagementService localUserManagementService,
        IIdentityBootstrapService bootstrapService)
    {
        _assignmentService = assignmentService;
        _localIdentityService = localIdentityService;
        _localUserManagementService = localUserManagementService;
        _bootstrapService = bootstrapService;
    }

    [HttpGet]
    public async Task<IActionResult> Login(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
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
        var model = await _assignmentService.BuildAccountAsync(User, cancellationToken);
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "PasswordForm")] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var model = await _assignmentService.BuildAccountAsync(User, cancellationToken);

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
    public async Task<IActionResult> SetClientAlias(SetClientAliasRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "The client alias could not be saved.";
            return RedirectToAction(nameof(Manage));
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

        return RedirectToAction(nameof(Manage));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
