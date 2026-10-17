using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Analytics;

namespace Portfolio.Web.Areas.Admin.Controllers;

public class AnalyticsController(IAnalyticsService analytics) : AdminController
{
    private static readonly int[] AllowedPeriods = [7, 30, 90];

    public async Task<IActionResult> Index(int days = 30, CancellationToken cancellationToken = default)
    {
        if (!AllowedPeriods.Contains(days))
        {
            days = 30;
        }

        return View(await analytics.GetOverviewAsync(days, cancellationToken));
    }
}
