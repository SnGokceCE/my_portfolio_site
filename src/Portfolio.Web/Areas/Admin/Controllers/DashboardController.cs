using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Analytics;
using Portfolio.Application.Dashboard;
using Portfolio.Application.SiteProfiles;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class DashboardController(
    ISiteProfileService profiles,
    IDashboardService dashboard,
    IAnalyticsService analytics) : AdminController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Profile"] = await profiles.GetAsync(cancellationToken);
        ViewData["Traffic"] = await analytics.GetOverviewAsync(7, cancellationToken);
        return View(await dashboard.GetStatsAsync(cancellationToken));
    }
}
