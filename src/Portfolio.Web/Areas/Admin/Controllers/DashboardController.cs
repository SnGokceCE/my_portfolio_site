using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Dashboard;
using Portfolio.Application.SiteProfiles;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class DashboardController(ISiteProfileService profiles, IDashboardService dashboard) : AdminController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Profile"] = await profiles.GetAsync(cancellationToken);
        return View(await dashboard.GetStatsAsync(cancellationToken));
    }
}
