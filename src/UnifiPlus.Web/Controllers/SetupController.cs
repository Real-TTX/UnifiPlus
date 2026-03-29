using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using UnifiPlus.Web.Models;
using UnifiPlus.Web.Services;

namespace UnifiPlus.Web.Controllers;

public sealed class SetupController : Controller
{
    private readonly IIdentityBootstrapService _bootstrapService;
    private readonly ILocalUserStore _localUserStore;
    private readonly IUniFiConfigurationStore _uniFiConfigurationStore;

    public SetupController(
        IIdentityBootstrapService bootstrapService,
        ILocalUserStore localUserStore,
        IUniFiConfigurationStore uniFiConfigurationStore)
    {
        _bootstrapService = bootstrapService;
        _localUserStore = localUserStore;
        _uniFiConfigurationStore = uniFiConfigurationStore;
    }

    [HttpGet]
    public async Task<IActionResult> Admin(CancellationToken cancellationToken)
    {
        if (!await _bootstrapService.IsAdminSetupRequiredAsync(cancellationToken))
        {
            return RedirectToAction("Login", "Account");
        }

        return View(new SetupAdminViewModel
        {
            UserStorePath = _localUserStore.StoragePath,
            ConnectionStorePath = _uniFiConfigurationStore.StoragePath
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Admin([Bind(Prefix = "Form")] CreateAdminRequest request, CancellationToken cancellationToken)
    {
        var model = new SetupAdminViewModel
        {
            UserStorePath = _localUserStore.StoragePath,
            ConnectionStorePath = _uniFiConfigurationStore.StoragePath
        };

        if (!ModelState.IsValid)
        {
            return View(model.WithForm(request));
        }

        var admin = await _bootstrapService.CreateInitialAdminAsync(request.UserId, request.Password, cancellationToken);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, admin.UserId),
            new("unifiplus:user_id", admin.UserId),
            new(ClaimTypes.Role, admin.Role)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToAction("Connection", "Admin");
    }
}
