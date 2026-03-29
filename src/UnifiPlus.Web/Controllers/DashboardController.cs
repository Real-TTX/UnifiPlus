using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiPlus.Web.Models;
using UnifiPlus.Web.Services;

namespace UnifiPlus.Web.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    private readonly IUniFiClientAssignmentService _assignmentService;

    public DashboardController(IUniFiClientAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _assignmentService.BuildDashboardAsync(User, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignClientRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
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

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateWan(UpdateWanRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
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

        return RedirectToAction(nameof(Index));
    }
}
