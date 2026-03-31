using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UnifiPlus.Web.Models;
using UnifiPlus.Web.Services;

namespace UnifiPlus.Web.Controllers;

[Authorize]
public sealed class BandwidthController : Controller
{
    private readonly IUniFiClientAssignmentService _assignmentService;
    private readonly IBandwidthTemplateStore _bandwidthTemplateStore;

    public BandwidthController(
        IUniFiClientAssignmentService assignmentService,
        IBandwidthTemplateStore bandwidthTemplateStore)
    {
        _assignmentService = assignmentService;
        _bandwidthTemplateStore = bandwidthTemplateStore;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dashboard = await _assignmentService.BuildDashboardAsync(User, cancellationToken);
        var templates = await _bandwidthTemplateStore.GetAsync(cancellationToken);
        return View(new BandwidthPageViewModel
        {
            UserId = dashboard.UserId,
            IsAdmin = dashboard.IsAdmin,
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
