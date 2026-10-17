using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.Common.Security;
using Portfolio.Application.SiteProfiles;

namespace Portfolio.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class DashboardController(ISiteProfileService profiles) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await profiles.GetAsync(cancellationToken);
        return View(profile);
    }
}
