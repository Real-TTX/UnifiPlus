using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiPlus.Web.Models;
using UnifiPlus.Web.Services;

namespace UnifiPlus.Web.Controllers;

[Authorize]
public sealed class BandwidthController : Controller
{
    private readonly IUniFiClientAssignmentService _assignmentService;

    public BandwidthController(IUniFiClientAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dashboard = await _assignmentService.BuildDashboardAsync(User, cancellationToken);
        return View(new BandwidthPageViewModel
        {
            UserId = dashboard.UserId,
            IsAdmin = dashboard.IsAdmin,
            AssignedClients = dashboard.AssignedClients
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateBandwidthRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ActionError"] = "Enter at least one valid upload or download limit.";
            return RedirectToAction(nameof(Index));
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

        return RedirectToAction(nameof(Index));
    }
}
