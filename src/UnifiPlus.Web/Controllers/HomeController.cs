using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiPlus.Web.Models;
using UnifiPlus.Web.Services;

namespace UnifiPlus.Web.Controllers;

public sealed class HomeController : Controller
{
    private readonly IUniFiClientAssignmentService _assignmentService;
    private readonly IUniFiApiClient _uniFiApiClient;

    public HomeController(IUniFiClientAssignmentService assignmentService, IUniFiApiClient uniFiApiClient)
    {
        _assignmentService = assignmentService;
        _uniFiApiClient = uniFiApiClient;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dashboard = await _assignmentService.BuildDashboardAsync(User, cancellationToken);
        var activeRules = User.IsInRole("Admin")
            ? await _uniFiApiClient.GetActiveRulesAsync(cancellationToken)
            : [];

        return View(new HomeDashboardViewModel
        {
            UserId = dashboard.UserId,
            IsAdmin = User.IsInRole("Admin"),
            UplinkCount = dashboard.AvailableWans.Count,
            AssignedClientCount = dashboard.AssignedClients.Count,
            TotalClientCount = dashboard.AllClients.Count,
            ActiveRuleCount = activeRules.Count
        });
    }

    [HttpGet]
    public IActionResult Error()
    {
        return View();
    }
}
